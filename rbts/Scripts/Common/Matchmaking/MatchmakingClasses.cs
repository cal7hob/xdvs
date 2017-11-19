using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using ExitGames.Client.Photon;

namespace Matchmaking
{
    public class RoomCountryInfo
    {
        private const int DATA_SIZE = 4 + 6;

        private static readonly byte[] buffer = new byte[DATA_SIZE];
        
        public string countryName; // Только 2 символа
        public int players;

        public RoomCountryInfo(string countryName, int players)
        {
            this.countryName = countryName;
            this.players = players;
        }

        public RoomCountryInfo()
        {
            countryName = "";
            players = 0;
        }

        public RoomCountryInfo(RoomCountryInfo other, int playerCountDelta)
        {
            countryName = other.countryName;
            players = other.players + playerCountDelta;
        }

        public bool DoesForCountry(string countryCode)
        {
            return string.IsNullOrEmpty(countryName) || MatchMaker.GetCorrectCountryForTeam(countryCode) == countryName;
        }

        public bool IsShared
        {
            get { return string.IsNullOrEmpty(countryName); }
        }

        public override string ToString()
        {
            return string.Format("RoomCountryInfo (country='{0}', players={1})", countryName, players);
        }

        public static short Serialize(StreamBuffer outStream, object customObject)
        {
            RoomCountryInfo obj = (RoomCountryInfo) customObject;
            
            byte[] bytes = new byte[DATA_SIZE];
            int index = 0;
            
            Protocol.Serialize(obj.players, bytes, ref index);
            EventInfo.SerializeString(obj.countryName, bytes, ref index);

            outStream.Write(bytes, 0, DATA_SIZE);
            return DATA_SIZE;
        }

        public static object Deserialize(StreamBuffer inStream, short length)
        {
            int index = 0;
            int players;

            inStream.Read(buffer, 0, DATA_SIZE);
            Protocol.Deserialize(out players, buffer, ref index);
            string countryName = EventInfo.DeserializeString(buffer, ref index);

            return new RoomCountryInfo(countryName, players);
        }
    }

    public class MatchmakerRule
    {
        private static Dictionary<int, string> botChanceKey = new Dictionary<int, string>
        {
            {-2, "BeforeLastRoomChance"},
            {-1, "LastRoomChance"},
            {0, "NormalRoomChance"},
            {1, "NextRoomChance" },
            {2, "AfterNextRoomChance" }
        };

        public ObscuredInt battlesLimit;
        public ObscuredInt beforeLastChance = 0;
        public ObscuredInt lastChance = 0;
        public ObscuredInt normalChance = 0;
        public ObscuredInt nextChance = 0;
        public ObscuredInt afterNextChance = 0;

        public Dictionary<int, Dictionary<BotDispatcher.BotBehaviours, int>> botTypeChances = new Dictionary<int, Dictionary<BotDispatcher.BotBehaviours, int>>();
        
        public static MatchmakerRule FromDictionary(Dictionary<string, object> dict)
        {
            JsonPrefs json = new JsonPrefs(dict);
            MatchmakerRule result = new MatchmakerRule
            {
                battlesLimit = json.ValueInt("battlesLimit"),
                beforeLastChance = json.ValueInt("beforeLastRoomChance"),
                lastChance = json.ValueInt("lastRoomChance"),
                normalChance = json.ValueInt("normalRoomChance"),
                nextChance = json.ValueInt("nextRoomChance"),
                afterNextChance = json.ValueInt("afterNextRoomChance"),
                botTypeChances = LoadBotTypeChances(json)
        };

            return result;
        }

        public override string ToString()
        {
            string result
                = string.Format(
                    "battlesLimit={0}, beforeLast={1}, last={2}, normal={3}, next={4}, afterNext={5}",
                    battlesLimit,
                    beforeLastChance,
                    lastChance,
                    normalChance,
                    nextChance,
                    afterNextChance);

            return result;
        }

        private static Dictionary<int, Dictionary<BotDispatcher.BotBehaviours, int>> LoadBotTypeChances(JsonPrefs json)
        {
            Dictionary<int, Dictionary<BotDispatcher.BotBehaviours, int>> output = new Dictionary<int, Dictionary<BotDispatcher.BotBehaviours, int>>();
            for (int roomDelta = -2; roomDelta <= 2; roomDelta++)
            {
                output[roomDelta] = new Dictionary<BotDispatcher.BotBehaviours, int>(3);
                foreach (BotDispatcher.BotBehaviours botType in Enum.GetValues(typeof (BotDispatcher.BotBehaviours)))
                {
                    if (botType == BotDispatcher.BotBehaviours.Tutorial)
                    {
                        continue;
                    }

                    string key = string.Format("{0}Bot{1}", botType.ToString().ToLower(), botChanceKey[roomDelta]);
                    output[roomDelta][botType] = json.ValueInt(key);
                }
            }

            return output;
        }
    }
}