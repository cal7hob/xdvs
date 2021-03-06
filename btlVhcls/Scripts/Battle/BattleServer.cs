using UnityEngine;
using System;
using System.Collections.Generic;
using System.Globalization;
using CodeStage.AntiCheat.ObscuredTypes;

namespace Http
{

    public class BonusDispatcher
    {
        public int bonusesInRoom = 4;
        public float bonusRefreshInterval = 60f;
        public int healthBonusChance = 10;
        public int fuelBonusChance = 10;
        public int attackBonusChance = 0;
        public int reloadBonusChance = 0;
        public int speedupBonusChance = 0;
        public int landmineBonusChance = 0;
        public int missileBonusChance = 0;

        public void reset()
        {
            bonusesInRoom = 4;
            bonusRefreshInterval = 60f;

            healthBonusChance = 10;
            fuelBonusChance = 10;
            attackBonusChance = 0;
            reloadBonusChance = 0;
            speedupBonusChance = 0;
            landmineBonusChance = 0;
            missileBonusChance = 0;
        }

        public void load (Dictionary<string, object> data)
        {
            var prefs = new JsonPrefs(data);

            bonusesInRoom = prefs.ValueInt("bonusesInRoom", 4);
            bonusRefreshInterval = prefs.ValueFloat("bonusRefreshInterval", 60f);

            healthBonusChance = prefs.ValueInt("healthBonusChance", 10);
            fuelBonusChance = prefs.ValueInt("fuelBonusChance", 10);
            attackBonusChance = prefs.ValueInt("attackBonusChance", 0);
            reloadBonusChance = prefs.ValueInt("reloadBonusChance", 0);
            speedupBonusChance = prefs.ValueInt("speedupBonusChance", 0);
            landmineBonusChance = prefs.ValueInt("landmineBonusChance", 0);
            missileBonusChance = prefs.ValueInt("missileBonusChance", 0);
        }
    }

    public class BattleOptions
    {
        public bool loaded = false;
        public BonusDispatcher bonusDispatcher = new BonusDispatcher ();

        public void reset ()
        {
            loaded = false;
            bonusDispatcher.reset ();
        }

        public void load(Dictionary<string, object> data)
        {
            loaded = true;
            var prefs = new JsonPrefs (data);
            bonusDispatcher.load (prefs.ValueObjectDict ("BonusDispatcher"));
        }
    }

    public class BattleResult
    {
        public int place = 100;
        public bool isProperBattleEnd = false;
        public bool isEnoughPlayers = false;
        public ProfileInfo.Price award = new ProfileInfo.Price(0, ProfileInfo.PriceCurrency.Silver);

        public int gold = 0;
        public int silverBase = 0;
        public int silverVIP = 0;
        public int experienceBase = 0;
        public int experienceVIP = 0;
        public int fuel = 0;
        public int speedBoosters = 0;
        public int reloadBoosters = 0;
        public int attackBoosters = 0;
        public int heals = 0;
        public int mines = 0;
        public int missiles = 0;
        public int shoots = 0;
        public int shootsSaclos = 0;
        public int shootsIrcm = 0;
        public int hits = 0;
        public int hitsSaclos = 0;
        public int takenHits = 0;
        public int takenHitsSaclos = 0;
        public int givenDamage = 0;
        public int givenDamageSaclos = 0;
        public int takenDamage = 0;
        public int takenDamageSaclos = 0;
        public int frags = 0;
        public int deaths = 0;
        public float mileage = 0f;
        public bool isBattleAsVip = false;
        public bool isDoubleExp = false;
        public bool isNewbie = false;
        public int maxKillsInARow = 0;

        public int Accuracy {
            get {
                return (int)((float)hits * 100f / (float)shoots);
            }
        }
        public int AccuracySaclos {
            get {
                return (int)((float)hitsSaclos * 100f / (float)shootsSaclos);
            }
        }

        public bool isAllQuestsCompleted = false;
        public Quest quest = null;
        public ProfileInfo.Price questAll = new ProfileInfo.Price(0, ProfileInfo.PriceCurrency.Gold);

        public void load (object dict)
        {
            JsonPrefs p = new JsonPrefs(dict);

            place = p.ValueInt("result/place");
            isProperBattleEnd = p.ValueBool("result/isProperBattleEnd");
            isEnoughPlayers = p.ValueBool("result/isEnoughPlayers");
            if (p.Contains("result/award")) {
                award = ProfileInfo.Price.FromDictionary(p.ValueObjectDict("result/award"));
            }

            gold = p.ValueInt("stats/gold");
            silverBase = p.ValueInt("stats/silverBase");
            silverVIP = p.ValueInt("stats/silverVIP");
            experienceBase = p.ValueInt("stats/experienceBase");
            experienceVIP = p.ValueInt("stats/experienceVIP");
            fuel = p.ValueInt("stats/fuel");
            speedBoosters = p.ValueInt("stats/speedBoosters");
            reloadBoosters = p.ValueInt("stats/reloadBoosters");
            attackBoosters = p.ValueInt("stats/attackBoosters");
            heals = p.ValueInt("stats/heals");
            mines = p.ValueInt("stats/mines");
            missiles = p.ValueInt("stats/missiles");
            shoots = p.ValueInt("stats/shoots");
            shootsSaclos = p.ValueInt("stats/shootsSaclos");
            shootsIrcm = p.ValueInt("stats/shootsIrcm");
            hits = p.ValueInt("stats/hits");
            hitsSaclos = p.ValueInt("stats/hitsSaclos");
            takenHits = p.ValueInt("stats/takenHits");
            takenHitsSaclos = p.ValueInt("stats/takenHitsSaclos");
            givenDamage = p.ValueInt("stats/givenDamage");
            givenDamageSaclos = p.ValueInt("stats/givenDamageSaclos");
            takenDamage = p.ValueInt("stats/takenDamage");
            takenDamageSaclos = p.ValueInt("stats/takenDamageSaclos");
            frags = p.ValueInt("stats/frags");
            deaths = p.ValueInt("stats/deaths");
            mileage = p.ValueFloat("stats/mileage");
            isBattleAsVip = p.ValueBool("stats/isBattleAsVip");
            isDoubleExp = p.ValueBool("stats/isDoubleExp");
            isNewbie = p.ValueBool("stats/isNewbie");
            maxKillsInARow = p.ValueInt("stats/maxKillsInARow");

            isAllQuestsCompleted = p.ValueBool("quest/isAllCompleted");
            if (p.Contains("quest/current/id")) {
                quest = Quest.CreateFromDictionary(p.ValueObjectDict("quest/current"));
            }
            if (p.Contains("quest/all/value"))
            {
                questAll = ProfileInfo.Price.FromDictionary(p.ValueObjectDict("quest/all"));
            }
        }
    }

    public class BattleServer : MonoBehaviour
    {
        public BattleOptions options = new BattleOptions();
        public BotNames botNames;
        public BattleResult result = new BattleResult();
        public Quest quest = null;
        public int sendEventsSeconds = 20;
        public int sendEventsRetrySeconds = 2;
        public int sendEventsRetryCount = 3;

        public bool IsWaitingForBattleEnd { get; private set; }

        protected int battleId = -1;
        protected int eventsCounter = 0;
        protected bool isBattleBegin = false;

        void Start ()
        {
            IsWaitingForBattleEnd = false;
            Dispatcher.Subscribe (EventId.ItemTaken, OnItemTaken);
        }

        void OnDestroy ()
        {
            Dispatcher.Unsubscribe (EventId.ItemTaken, OnItemTaken);
        }

        public void PrepareToBattle ()
        {
            battleId = -1;
            eventsCounter = 0;
            options.reset();
            result = new BattleResult();
            quest = null;
        }

        public void StartBattle (Room room, VehicleUpgrades vehicleUpgrades, Dictionary<VehicleInfo.VehicleParameter, ObscuredFloat> vehicleParameters, Dictionary<int, ObscuredInt> consumables, bool isCreateRoom, bool roomWasFulled, Action<bool> result = null)
        {
            isBattleBegin = roomWasFulled;

            var data = new Dictionary<string, object> ();
            data["tank"] = vehicleUpgrades.ToDictionary ();
            data["room"] = new Dictionary<string, object> () {
                    {"name",        room.Name},
                    {"maxPlayers",  room.MaxPlayers},
                    {"isOpen",      room.IsOpen},
                    {"isVisible",   room.IsVisible},
                    {"summary",     room.ToStringFull ()}
                };
            data["map"] = Enum.Parse(typeof(GameManager.MapId), room.CustomProperties["mp"].ToString()).ToString();
            data["mapId"] = room.CustomProperties["mp"];
            data["player"] = new Dictionary<string, object> () {
                    {"teamId",          GameManager.MatchedTeam},
                    {"isVip",           ProfileInfo.IsPlayerVip},
                    {"isNewbie",        ProfileInfo.IsNewbie},
                    {"level",           ProfileInfo.Level},
                    {"armor",           vehicleParameters[VehicleInfo.VehicleParameter.Armor]},
                    {"damage",          vehicleParameters[VehicleInfo.VehicleParameter.Damage]},
                    {"rocketDamage",    vehicleParameters[VehicleInfo.VehicleParameter.RocketDamage]},
                    {"rof",             vehicleParameters[VehicleInfo.VehicleParameter.RoF]},
                    {"ircmRof",         vehicleParameters[VehicleInfo.VehicleParameter.IRCMRoF]},
                    {"speed",           vehicleParameters[VehicleInfo.VehicleParameter.Speed]},
                };
            data["level"] = room.CustomProperties["lv"];
            data["mode"] = room.CustomProperties["gm"];
            data["fuel"] = ProfileInfo.FuelRequired;
            data["punVersion"] = PhotonNetwork.versionPUN;
            data["protocolVersion"] = GameManager.PhotonRoomVersion;
            data["isCreateRoom"] = isCreateRoom;
            data["consumablesInBattle"] = CreateConsumablesDict(consumables);

            data["timeStarted"] = isBattleBegin ? 0 : -1;
            data["ping"] = PhotonNetwork.GetPing();

            var request = Manager.Instance().CreateRequest("/battle/start");
            request.Form.AddField("timestamp", ((long)GameData.CurrentTimeStamp).ToString(CultureInfo.InvariantCulture));
            request.Form.AddField("json", data.ToJsonString());

            Manager.StartAsyncRequest(request,
                // Success
                delegate (Response r) {
                    battleId = r.Prefs.ValueInt("battleId", -1);
                    if (r.Prefs.Contains("options")) {
                        options.load(r.Prefs.ValueObjectDict("options"));
                    }
                    botNames = new BotNames(r.Prefs);
                    if (r.Prefs.Contains("quest")) {
                        quest = Quest.CreateFromDictionary(r.Prefs.ValueObjectDict("quest"));

                    }
                    StartControllerTracking();
                    Rewired.ReInput.controllers.AddLastActiveControllerChangedDelegate(ActiveControllerChanged);
                    if (result != null) {
                        result(true);
                    }
                    m_canSendEvents = sendEventsSeconds;
                },
                // Fail
                delegate (Response r) {
                    if (result != null) {
                        result(false);
                    }
                });
        }

        public void EndBattle (string disconnectCause, Action<bool> finished = null)
        {
            if (battleId < 0) {
                return;
            }

            Rewired.ReInput.controllers.RemoveLastActiveControllerChangedDelegate (ActiveControllerChanged);
            StopControllerTracking ();

            var data = new Dictionary<string, object> {
                {"place", StatTable.MyVehicleRank},
                {"isProperBattleEnd", BattleStatisticsManager.BattleStats["ProperEndBattle"]},
                {"isPhotonDisconnect", BattleStatisticsManager.BattleStats["PhotonDisconnect"]},
                {"isEnoughPlayers", StatTable.IsEnoughPlayers},
                {"disconnectCause", disconnectCause },
                {"controllerStats", m_stats }
            };

            if (GameData.Mode == GameData.GameMode.Team) {
                data["myTeamScore"] = ScoreCounter.FriendTeamScore;
                data["enemyTeamScore"] = ScoreCounter.EnemyTeamScore;
            }

            IsWaitingForBattleEnd = true;
            DoRequest ("/battle/end", data, 
                (Response r) => { // Success
                    result.load(r.Data);
                    IsWaitingForBattleEnd = false;
                    if (finished != null) {
                        finished(true);
                    }
                },
                (Response r) => { // Fail
                    IsWaitingForBattleEnd = false;
                    Debug.LogError("Battle result load failed!");
                    if (finished != null) {
                        finished(false);
                    }
                }
            );
            m_canSendEvents = -1;
        }

        /// <summary>
        /// ���������� ������� � ������� ������� ���
        /// </summary>
        /// <param name="secondsFromStart">�� ����� ������� �� ������ ��� ������� ������</param>
        public void StartTimer (int secondsFromStart)
        {
            if (isBattleBegin) return;
            isBattleBegin = true;
            DoRequest("/battle/begin", new Dictionary<string, object>() {
                {"timeStarted", secondsFromStart},
            });
        }

        public void Kill (int victimId, int experience)
        {
            if (ProfileInfo.IsBattleTutorial)
            {
                return;
            }

            DoRequest ("/battle/kill", 
                new Dictionary<string, object>() {
                    {"victimId", victimId},
                    {"experience", experience}
                }
            );
        }

        public void Death (int killerId)
        {
            DoRequest ("/battle/death", new Dictionary<string, object> {
                    {"killerId", killerId}
                }
            );
        }

        public void UseConsumable (int consumableId) {
            DoRequest ("/battle/useConsumable", new Dictionary<string, object> {
                    {"consumableId", consumableId}
            });
        }

        public void BuyRespawnBonus (BonusItem.BonusType type, int gold)
        {
            var price = new ProfileInfo.Price (gold, ProfileInfo.PriceCurrency.Gold);
            DoRequest ("/battle/buyBonus", new Dictionary<string, object> (){
                {"type", type},
                {"price", price.ToDictionary ()}
            });
        }

        public void BuyGameProlongation (int gold)
        {
            var price = new ProfileInfo.Price (gold, ProfileInfo.PriceCurrency.Gold);
            DoRequest ("/battle/buyProlongation", new Dictionary<string, object> () {
                {"duration", GameData.ProlongTimeAddition},
                {"price", price.ToDictionary ()}
            });
        }

        public void BuyHastenRespawn (Action<bool> result = null)
        {
            DoRequest ("/battle/buyHastenRespawn", new Dictionary<string, object> () {});
        }

        private void OnItemTaken (EventId id, EventInfo ei)
        {
            if (ProfileInfo.IsBattleTutorial)
                return;

            EventInfo_III info = (EventInfo_III)ei;

            var bonusType = (BonusItem.BonusType)info.int1;
            var amount = info.int2;

            if (info.int3 != BattleController.MyPlayerId)
                return;

            DoRequest ("/battle/takeItem", new Dictionary<string, object> () {
                    {"item", bonusType.ToString ()},
                    {"amount", amount}
                }
            );
        }

        private void DoRequest (string path, Dictionary<string, object> data, Request.WWWResultCallback success = null, Request.WWWResultCallback fail = null)
        {
            var stats = new Dictionary<string, object>();
            if (BattleController.Instance && BattleController.MyVehicle)
            {
                stats["odometer"] = BattleController.MyVehicle.Odometer;
            }
            else {
                stats["odometer"] = BattleStatisticsManager.BattleStats["Mileage"];
            }
            stats["shoots"] = BattleStatisticsManager.BattleStats["Shoots"];
            stats["shootsSaclos"] = BattleStatisticsManager.BattleStats["Shoots_SACLOS"];
            stats["shootsIrcm"] = BattleStatisticsManager.BattleStats["Shoots_IRCM"];
            stats["shootsAtgw"] = BattleStatisticsManager.BattleStats["Shoots_ATGW"];
            stats["shootsAgs"] = BattleStatisticsManager.BattleStats["Shoots_AGS"];
            stats["shootsMachinegun"] = BattleStatisticsManager.BattleStats["Shoots_MachineGun"];
            stats["hits"] = BattleStatisticsManager.BattleStats["Hits"];
            stats["hitsSaclos"] = BattleStatisticsManager.BattleStats["Hits_SACLOS"];
            stats["hitsAtgw"] = BattleStatisticsManager.BattleStats["Hits_ATGW"];
            stats["hitsAgs"] = BattleStatisticsManager.BattleStats["Hits_AGS"];
            stats["hitsMachinegun"] = BattleStatisticsManager.BattleStats["Hits_MachineGun"];
            stats["takenHits"] = BattleStatisticsManager.BattleStats["TakenHits"];
            stats["takenHitsSaclos"] = BattleStatisticsManager.BattleStats["TakenHits_SACLOS"];
            stats["takenHitsAtgw"] = BattleStatisticsManager.BattleStats["TakenHits_ATGW"];
            stats["takenHitsAgs"] = BattleStatisticsManager.BattleStats["TakenHits_AGS"];
            stats["takenHitsMachinegun"] = BattleStatisticsManager.BattleStats["TakenHits_MachineGun"];
            stats["givenDamage"] = BattleStatisticsManager.BattleStats["GivenDamage"];
            stats["takenDamage"] = BattleStatisticsManager.BattleStats["TakenDamage"];
            stats["givenDamageSaclos"] = BattleStatisticsManager.BattleStats["GivenDamage_SACLOS"];
            stats["givenDamageAtgw"] = BattleStatisticsManager.BattleStats["GivenDamage_ATGW"];
            stats["givenDamageAgs"] = BattleStatisticsManager.BattleStats["GivenDamage_AGS"];
            stats["givenDamageMachinegun"] = BattleStatisticsManager.BattleStats["GivenDamage_MachineGun"];
            stats["takenDamageSaclos"] = BattleStatisticsManager.BattleStats["TakenDamage_SACLOS"];
            stats["takenDamageAtgw"] = BattleStatisticsManager.BattleStats["TakenDamage_ATGW"];
            stats["takenDamageAgs"] = BattleStatisticsManager.BattleStats["TakenDamage_AGS"];
            stats["takenDamageMachinegun"] = BattleStatisticsManager.BattleStats["TakenDamage_MachineGun"];
            data["stats"] = stats;
            data["ping"] = PhotonNetwork.GetPing();
            data["timestamp"] = (long)GameData.CurrentTimeStamp;
            data["timeInBattle"] = (int)BattleController.TimeInBattleUnity;
            data["timeInBattlePhoton"] = (int)BattleController.TimeInBattlePhoton;
            data["path"] = path;
            data["eventId"] = eventsCounter++;

            m_events.AddLast (data.ToJsonString());

            if (success != null) cbSuccess = success;
            if (fail != null) cbFail = fail;
        }

        #region Battle Events batching
        LinkedList<string> m_events = new LinkedList<string>();
        int m_eventsSendingCount = 0;
        double m_canSendEvents = 0f;
        bool m_isSendingActive = false;
        Request.WWWResultCallback cbSuccess = null;
        Request.WWWResultCallback cbFail = null;

        private void Update()
        {
            if (battleId <= 0) return;

            m_canSendEvents -= Time.unscaledDeltaTime;
            //ConsoleProDebug.Watch("[BattleServer]canSendEvents", m_canSendEvents.ToString());
            if ( (m_canSendEvents < 0f) && !m_isSendingActive && (m_events.Count > 0) ) {
                m_eventsSendingCount = m_events.Count;

                var request = Manager.Instance().CreateRequest("/battle/events");
                request.Form.AddField("timestamp", ((long)GameData.CurrentTimeStamp).ToString(CultureInfo.InvariantCulture));
                if (battleId > 0) {
                    request.Form.AddField("battleId", battleId.ToString(CultureInfo.InvariantCulture));
                }
                foreach (var e in m_events) {
                    request.Form.AddField("events[]", e);
                }
                //for (int i = 0; i < m_events.Count; i++) {
                //    request.Form.AddField("events[]", m_events[i]);
                //}

                m_isSendingActive = true;
                Manager.StartAsyncRequest(request, 
                    (r) => { // Success
                        m_isSendingActive = false;
                        m_canSendEvents = sendEventsSeconds;

                        for (int i = 0; i < m_eventsSendingCount; i++) {
                            m_events.RemoveFirst();
                        }
                        m_eventsSendingCount = 0;

                        try {
                            if (r.Prefs.Contains("quest")) {
                                if (r.Prefs.Contains("quest/current")) {
                                    var quest = r.Prefs.ValueObjectDict("quest/current", null);
                                    if (quest != null) {
                                        CheckQuestUpdate(Quest.CreateFromDictionary(quest));
                                    }
                                }
                                else {
                                    CheckQuestUpdate(Quest.CreateFromDictionary(r.Prefs.ValueObjectDict("quest")));
                                }
                            }
                        }
                        catch (Exception e) {
                            Debug.LogErrorFormat("Can't update quest: {0}\n{1}", e.Message, r.Prefs.ValueObjectDict("quest").ToJsonString());
                            Debug.LogException(e);
                        }

                        if (cbSuccess != null) {
                            cbSuccess(r);
                            cbSuccess = null;
                        }
                    }, 
                    (r) => { // Fail
                        m_isSendingActive = false;
                        m_canSendEvents = sendEventsRetrySeconds;

                        if (IsWaitingForBattleEnd) {
                            sendEventsRetryCount--;
                        }

                        if (sendEventsRetryCount < 0) {
                            m_events.Clear();

                            if (cbFail != null) {
                                cbFail(r);
                                cbFail = null;
                            }
                        }
                    }
                );

            }
        }
        #endregion

        private void CheckQuestUpdate (Quest q)
        {
            if ( (q.progress != quest.progress) || (q.isComplete != quest.isComplete) ) {
                if (q.type != Quest.Type.Mileage) {
                    bool isCompleted = q.isComplete && !quest.isComplete;
                    quest = q;
                    Dispatcher.Send(EventId.BattleQuestUpdated, new EventInfo_SimpleEvent());
                    if (isCompleted) {
                        Dispatcher.Send(EventId.QuestCompleted, new EventInfo_I((int)quest.type));
                    }
                }
            }
        }

        private Dictionary<string, object> CreateConsumablesDict(Dictionary<int, ObscuredInt> dict)
        {
            Dictionary<string, object> output = new Dictionary<string, object>(dict.Count);
            foreach (var consPair in dict)
            {
                output.Add(consPair.Key.ToString(), consPair.Value);
            }

            return output;
        }

        #region Controller Type Logger

        class ControllerTime {
            public bool isActive = false;
            public float activeTime = 0f;
            public float lastActivationTime = 0f;
        }

        float m_trackingStartTime;
        bool m_isMouseActive = false;
        Dictionary<Rewired.ControllerType, ControllerTime> m_controllers;
        Dictionary<string, object> m_stats = new Dictionary<string, object> ();

        void StartControllerTracking () {
            m_trackingStartTime = Time.realtimeSinceStartup;
            m_controllers = new Dictionary<Rewired.ControllerType, ControllerTime> ();
            foreach (Rewired.ControllerType cType in Enum.GetValues (typeof (Rewired.ControllerType))) {
                var cData = new ControllerTime ();
                m_controllers[cType] = cData;
            }

            ActiveControllerChanged (Rewired.ReInput.controllers.GetLastActiveController ());
        }

        void StopControllerTracking () {
            ActiveControllerChanged (Rewired.ReInput.controllers.GetLastActiveController ());
            m_stats = new Dictionary<string, object> ();
            float sessionTime = Time.realtimeSinceStartup - m_trackingStartTime;
            foreach (var kvp in m_controllers) {
                if (!Mathf.Approximately (0f, kvp.Value.activeTime)) {
                    m_stats[kvp.Key.ToString ()] = kvp.Value.activeTime / sessionTime;
                }
            }
        }

        void ActiveControllerChanged (Rewired.Controller controller) {
            if (m_isMouseActive && controller.type == Rewired.ControllerType.Custom) {
                return;
            }
            foreach (var kvp in m_controllers) {
                if (kvp.Value.isActive) {
                    kvp.Value.activeTime += Time.realtimeSinceStartup - kvp.Value.lastActivationTime;
                    kvp.Value.isActive = false;
                }
            }
            m_controllers[controller.type].isActive = true;
            m_controllers[controller.type].lastActivationTime = Time.realtimeSinceStartup;
            m_isMouseActive = controller.type == Rewired.ControllerType.Mouse;
        }


        #endregion
    }
}