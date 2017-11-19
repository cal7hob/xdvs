using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeStage.AntiCheat.ObscuredTypes;
using Disconnect;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Matchmaking
{
    public static class MatchMaker
    {
        private const int BEFORE_PREVIOUS_GROUP_DELTA = -2;
        private const int PREVIOUS_GROUP_DELTA = -1;
        private const int CURRENT_GROUP_DELTA = 0;
        private const int NEXT_GROUP_DELTA = 1;
        private const int AFTER_NEXT_GROUP_DELTA = 2;
        private const string REGULAR_TEAM_COUNTRY_SYMBOL = "**";

        public const int SANDBOX_MATCHMAKING_GROUP = 100;
        public const int CHEATER_MATCHMAKING_GROUP = 999;
        public const int SANDBOX_VEHICLE_GROUP = 1;

        private static List<MatchmakerRule> matchmakerRules;
        private static MatchmakerRule currentRule;
        private static HashSet<string> specialCountries = new HashSet<string>();
        private static int MaxPlayersPerTeam { get { return GameData.maxPlayers / 2; } }

        public static void SetMatchmakerRules(List<object> list)
        {
            if (list == null)
            {
                Debug.LogError("No matchmaker rules loaded!");
                return;
            }

            matchmakerRules = new List<MatchmakerRule>(list.Count);
            foreach (object obj in list)
                matchmakerRules.Add(MatchmakerRule.FromDictionary((Dictionary<string, object>)obj));
        }

        public static void SetSpecialCountries(HashSet<string> countries)
        {
            if (countries != null)
                specialCountries = countries;
        }

        public static string SelectRoom(int mapId, bool createIfNeeded, string lastSelectedRoom, out int playersCount,
            int group)
        {
            if (!PhotonNetwork.insideLobby)
            {
                playersCount = -1;
                return null;
            }

            Dictionary<int, List<RoomInfo>> roomDict = new Dictionary<int, List<RoomInfo>>(5);
            RoomInfo[] rooms = PhotonNetwork.GetRoomList();
            if (!string.IsNullOrEmpty(lastSelectedRoom))
            {
                foreach (RoomInfo roomInfo in rooms)
                {
                    if (roomInfo.Name == lastSelectedRoom && TotalPlacesBusyInRoom(roomInfo) < GameData.maxPlayers)
                    {
                        playersCount = TrimmedVehicleCountInRoom(roomInfo);
                        return lastSelectedRoom;
                    }
                }
            }
            rooms = rooms.OrderBy(x => TotalPlacesBusyInRoom(x)).ToArray();
            foreach (RoomInfo roomInfo in rooms)
            {
                int busyPlacesInRoom = TotalPlacesBusyInRoom(roomInfo);
                if (
                    (int)roomInfo.CustomProperties["mp"] != mapId
                    || !roomInfo.IsOpen
                    || (int)roomInfo.CustomProperties["gm"] != (int)GameData.Mode
                    || busyPlacesInRoom >= GameData.maxPlayers
                    || !RoomIsOpenForMe(roomInfo)
                    )
                    continue;

                int level = (int)roomInfo.CustomProperties["lv"];

                if (!roomDict.ContainsKey(level))
                    roomDict.Add(level, new List<RoomInfo>(5));

                roomDict[level].Add(roomInfo);
            }

            int selectedGroup = GetMatchmakingGroup(group);

            if (!roomDict.ContainsKey(selectedGroup))
            {
                if (!createIfNeeded)
                {
                    playersCount = 0;
                    return null;
                }
                if (CreateRoom(mapId, selectedGroup))
                {
                    var mapName = Enum.Parse(typeof(GameManager.MapId), mapId.ToString()).ToString();

                    //Manager.ReportStats(
                    //    location: "matchmaker",
                    //    action: "createRoom",
                    //    query: new Dictionary<string, string>
                    //    {
                    //        { "level", properties["lv"].ToString() },
                    //        { "mapId", properties["mp"].ToString() },
                    //        { "mapName", mapName },
                    //        { "mode", properties["gm"].ToString() },
                    //    });

                    #region Google Analytics: joining battle

                    GoogleAnalyticsWrapper.LogEvent(
                        new CustomEventHitBuilder()
                            .SetParameter(GAEvent.Category.JoinBattle)
                            .SetParameter<GAEvent.Action>()
                            .SetSubject(GAEvent.Subject.MapName, mapName)
                            .SetParameter<GAEvent.Label>()
                            .SetSubject(GAEvent.Subject.VehicleID, ProfileInfo.CurrentVehicle)
                            .SetValue(ProfileInfo.Level));

                    GoogleAnalyticsWrapper.LogEvent(
                        new CustomEventHitBuilder()
                            .SetParameter(GAEvent.Category.JoinBattle)
                            .SetParameter<GAEvent.Action>()
                            .SetSubject(GAEvent.Subject.MapName, mapName)
                            .SetParameter<GAEvent.Label>()
                            .SetSubject(GAEvent.Subject.PlayerLevel, ProfileInfo.Level)
                            .SetValue(Convert.ToInt64(ProfileInfo.Fuel)));

                    GoogleAnalyticsWrapper.LogEvent(
                        new CustomEventHitBuilder()
                            .SetParameter(GAEvent.Category.JoinBattle)
                            .SetParameter<GAEvent.Action>()
                            .SetSubject(GAEvent.Subject.MapName, mapName)
                            .SetParameter<GAEvent.Label>()
                            .SetSubject(GAEvent.Subject.GameMode, GameData.Mode)
                            .SetValue(ProfileInfo.Level));

                    GoogleAnalyticsWrapper.LogScreen(GAScreens.Battle);

                    #endregion
                }
                playersCount = 0;
                return null;
            }
            //List<RoomInfo>
            RoomInfo suitableRoom = GetBestRoomFromMatched(roomDict[selectedGroup], GetCorrectCountryForTeam(ProfileInfo.CountryCode));
            playersCount = TrimmedVehicleCountInRoom(suitableRoom);
            return suitableRoom.Name;
        }
        
        public static bool CreateRoom(int mapId, int selectedGroup)
        {
            Hashtable properties = new Hashtable
            {
                /* **Видимые в коридоре** */
                {"lv", selectedGroup}, // Room level.
                {"mp", mapId}, // Map ID
                {"gm", (int) GameData.Mode}, // Game mode.
                {"rp", 0}, // Reserved places - количество забронированных мест в комнате на "отвалившихся"
                {"cntr", new[]{ new RoomCountryInfo(), new RoomCountryInfo() }}, //Countries - инфа по представителям спецстран в комнате
                {"bcn", 0 }, //Bot count - счетчик ботов в комнате
                /* **Невидимые в коридоре** */
                {"ct", PhotonNetwork.time}, // Creation time.
                {"nbr", 0.0}, // Next bonus refresh time.
                {"bc", 0}, // Map bonus count.
                {"eid", 1}, // Current effect ID (autoincrement).
                {"stake", 0}, // Total gold rush stake.
                {"goldLeader", 0}, // Player with gold (gold rush leader).
                {"awardPermission", false}, // Is gold rush award activated?
                {"lm", -1} // Last master's id (while master is forced disconnected)
            };

            return PhotonNetwork.CreateRoom(
                roomName: null,
                roomOptions: new RoomOptions
                {
                    IsVisible = true,
                    IsOpen = false,
                    EmptyRoomTtl = 1,
                    MaxPlayers = (byte)GameData.maxPlayers,
                    CustomRoomProperties = properties,
                    DeleteNullProperties = true,
                    CustomRoomPropertiesForLobby = new[] { "mp", "lv", "gm", "rp", "cntr", "bcn" }
                },
                typedLobby: null);
        }

        public static int GetTeamForNewPlayer(VehicleData playerData)
        {
            int teamId = GetTeamIdForNewPlayer(playerData);
            if (teamId >= 0)
                CreateTeamIfNeeded(GetCorrectCountryForTeam(playerData.country), teamId, BotDispatcher.IsBotId(playerData.playerId));
            return teamId;
        }

        public static int[] CountTeamMembers()
        {
            int[] teamMembers = {0, 0};
            foreach (var vehicle in BattleController.allVehicles.Values)
            {
                if (vehicle.data.teamId == 0)
                    teamMembers[0]++;
                else
                    teamMembers[1]++;
            }

            //Также подсчет участников команд, на которых зарезервированы места в комнате
            if (PhotonNetwork.room.CustomProperties.ContainsKey("bs"))
            {
                PlayerDisconnectInfo[] busy = (PlayerDisconnectInfo[]) PhotonNetwork.room.CustomProperties["bs"];
                if (busy != null)
                {
                    foreach (var bs in busy)
                    {
                        if (bs == null)
                            continue;

                        if (bs.TeamId == 0)
                            teamMembers[0]++;
                        else
                            teamMembers[1]++;
                    }
                }
            }

            return teamMembers;
        }

        public static int TotalBotsInRoom(RoomInfo roomInfo)
        {
            return (int)roomInfo.CustomProperties["bcn"];
        }

        public static int TotalBotsInThisRoom()
        {
            Room room = PhotonNetwork.room;
            if (room == null)
            {
                Debug.LogError("PhotonNetwork.room == NULL");
                return -1;
            }
            return (int)room.CustomProperties["bcn"];
        }

        public static bool KickBotFromTeam(int teamId)
        {
            if (TotalBotsInThisRoom() <= GameData.minBotCount)
                return false;

            foreach (VehicleController veh in BattleController.allVehicles.Values)
            {
                if (veh.IsBot && veh.data.teamId == teamId)
                {
                    BotDispatcher.Instance.RemoveBot(veh, true);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Обозначение страны для комнаты 
        /// </summary>
        /// <param name="countryCode"></param>
        /// <returns>строка страны для комнаты ('**' для обычных стран)</returns>
        public static string GetCorrectCountryForTeam(string countryCode)
        {
            return GameData.Mode == GameData.GameMode.Team ?
                IfCountryIsSpecial(countryCode) ?
                        countryCode :
                        REGULAR_TEAM_COUNTRY_SYMBOL
                    : "";
        }

        public static bool EmptyTeamsPresent(RoomCountryInfo[] cntr)
        {
            if (!CheckRoomCountryInfoArray(cntr))
                return true;

            return cntr.Any(x => x.IsShared);
        }

        private static int GetTeamIdForNewPlayer(VehicleData playerData)
        {
            if (GameData.Mode != GameData.GameMode.Team)
                return 0;

            PlayerDisconnectInfo info = BattleConnectManager.Instance.GetDisconnectInfoForPlayer(playerData.profileId);
            if (info != null)
                return info.TeamId;

            RoomCountryInfo[] countryInfos = PhotonNetwork.room.CustomProperties["cntr"] as RoomCountryInfo[];
            if (countryInfos == null)
                return 0;

            if (!CheckRoomCountryInfoArray(countryInfos))
                return -1;

            int[] teamMembers = CountTeamMembers();
            int supposedTeamId = teamMembers[0] > teamMembers[1] ? 1 : 0;

            if (BotDispatcher.IsBotId(playerData.playerId))
                return supposedTeamId; //Бота сразу в нуждающуюся команду

            string thisCounty = playerData.country;

            // Для спецстраны
            if (IfCountryIsSpecial(playerData.country))
            {
                for (int i = 0; i < countryInfos.Length; i++)
                {
                    if (countryInfos[i].DoesForCountry(thisCounty))
                        return teamMembers[i] < MaxPlayersPerTeam || KickBotFromTeam(i) ? i : -1;
                }

                return -1; //Ни одна команда не подходит
            }

            // Далее - для обычной страны

            if (countryInfos[supposedTeamId].IsShared || countryInfos[supposedTeamId].DoesForCountry(thisCounty))
            {
                if (teamMembers[supposedTeamId] < MaxPlayersPerTeam || KickBotFromTeam(supposedTeamId))
                    return supposedTeamId;
            }
            
            supposedTeamId = 1 - supposedTeamId; // Анализируем вторую команду

            if (countryInfos[supposedTeamId].IsShared || countryInfos[supposedTeamId].DoesForCountry(thisCounty))
            {
                if (teamMembers[supposedTeamId] < MaxPlayersPerTeam || KickBotFromTeam(supposedTeamId))
                    return supposedTeamId;
            }

            return -1;
        }

        private static void CreateTeamIfNeeded(string country, int teamId, bool forBot)
        {
            RoomCountryInfo[] cntr = PhotonNetwork.room.CustomProperties["cntr"] as RoomCountryInfo[];
            if (!cntr[teamId].IsShared)
                return;

            if (!forBot)
                cntr[teamId].countryName = country;
            PhotonNetwork.room.SetCustomProperties(new Hashtable {{"cntr", cntr}});
        }

        private static int GetMatchmakingGroup(int vehicleGroup)
        {
            BotDispatcher.botTypePriorities = matchmakerRules[0].botTypeChances[-2];

            if (ProfileInfo.IsCheater)
                return CHEATER_MATCHMAKING_GROUP;

            if ((vehicleGroup == SANDBOX_VEHICLE_GROUP && ProfileInfo.Level <= GameData.playerSandboxLevel))
                return SANDBOX_MATCHMAKING_GROUP;

            currentRule = SelectMatchmakerRule();

            int maxMatchmakingGroup =
                SafeLinq.Max(VehiclePool.Instance.Items.Select(vehicle => (int)vehicle.vehicleGroup))
                    + AFTER_NEXT_GROUP_DELTA;

            int[] priorities = new int[maxMatchmakingGroup];

            for (int i = 1; i <= maxMatchmakingGroup; i++)
                priorities[i - 1] = CalcGroupPriority(currentRule, vehicleGroup, i);

            int selectedGroup = MiscTools.GetRandomIndex(priorities) + 1;
            int currentGroupDelta = vehicleGroup - selectedGroup;
            BotDispatcher.botTypePriorities = currentRule.botTypeChances[currentGroupDelta];

            return selectedGroup;
        }

        private static int TrimmedVehicleCountInRoom(RoomInfo roomInfo)
        {
            int result = roomInfo.PlayerCount + RoomInfoManager.GetPlaceReserve(roomInfo) + TotalBotsInRoom(roomInfo);
            return result < GameData.maxPlayers ? result : GameData.maxPlayers - 1;
        }

        private static RoomInfo GetBestRoomFromMatched(List<RoomInfo> rooms, string countryCode)
        {
            if (rooms == null || rooms.Count == 0)
                return null;

            RoomInfo room = rooms[0];
            int countryMajority = CalcCountryMajorityForRoom(room, countryCode);
            int bestCountryMajority = countryMajority;
            for (int i = 1; i < rooms.Count; i++)
            {
                if ((countryMajority = CalcCountryMajorityForRoom(rooms[i], countryCode)) < bestCountryMajority)
                {
                    bestCountryMajority = countryMajority;
                    room = rooms[i];
                }
            }
            return room;
        }

        private static int TotalPlacesBusyInRoom(RoomInfo roomInfo)
        {
            int busy = roomInfo.PlayerCount + RoomInfoManager.GetPlaceReserve(roomInfo);
            if (GameData.isBotsEnabled)
                busy += GameData.minBotCount;

            return busy;

        }

        private static bool RoomIsOpenForMe(RoomInfo roomInfo)
        {
            if ((int)roomInfo.CustomProperties["gm"] == (int)GameData.GameMode.Deathmatch)
                // В deathmatch-комнаты заходят все страны
                return true;

            RoomCountryInfo[] countryInfo = (RoomCountryInfo[])roomInfo.CustomProperties["cntr"];
            return countryInfo.Any(
                x =>
                x.DoesForCountry(ProfileInfo.CountryCode)
                && x.players < MaxPlayersPerTeam
            );
        }

        private static int CalcCountryMajorityForRoom(RoomInfo roomInfo, string countryCode)
        {
            if (GameData.Mode != GameData.GameMode.Team)
                return TotalPlacesBusyInRoom(roomInfo);

            RoomCountryInfo[] countries = (RoomCountryInfo[])roomInfo.CustomProperties["cntr"];
            int countryPlayers = GameData.maxPlayers;
            int otherCountryPlayers = 0;
            for (int i = 0; i < countries.Length; i++)
            {
                RoomCountryInfo country = countries[i];
                if (country.DoesForCountry(countryCode))
                {
                    countryPlayers = Mathf.Min(countryPlayers, country.players);
                }
                else
                    otherCountryPlayers = Math.Max(country.players, otherCountryPlayers);
            }

            return countryPlayers - otherCountryPlayers;
        }

        private static MatchmakerRule SelectMatchmakerRule()
        {
            int battlesAmount = ProfileInfo.vehicleUpgrades[ProfileInfo.currentVehicle].battlesCount;

            foreach (MatchmakerRule matchmakerRule in matchmakerRules)
                if (battlesAmount < matchmakerRule.battlesLimit)
                    return matchmakerRule;

            return matchmakerRules.Last();
        }

        private static int CalcGroupPriority(MatchmakerRule rule, int ownGroup, int roomLevel)
        {
            int groupDelta = roomLevel - ownGroup;

            switch (groupDelta)
            {
                case BEFORE_PREVIOUS_GROUP_DELTA:
                    return rule.beforeLastChance;
                case PREVIOUS_GROUP_DELTA:
                    return rule.lastChance;
                case CURRENT_GROUP_DELTA:
                    return rule.normalChance;
                case NEXT_GROUP_DELTA:
                    return rule.nextChance;
                case AFTER_NEXT_GROUP_DELTA:
                    return rule.afterNextChance;
                default:
                    return 0;
            }
        }

        private static bool IfCountryIsSpecial(string countryCode)
        {
            return specialCountries.Contains(countryCode);
        }

        private static bool CheckRoomCountryInfoArray(RoomCountryInfo[] cntr)
        {
            if (cntr == null)
            {
                Debug.LogError("RoomCountryInfo[] is NULL");
                return false;
            }
            if (cntr.Length < 2)
            {
                Debug.LogError("Incorrect RoomCountryInfo[] length");
                return false;
            }
            return true;
        }
    }
}