using UnityEngine;
using System;
using System.Collections.Generic;
using System.Globalization;
using CodeStage.AntiCheat.ObscuredTypes;
using XD;

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

        public void load(Dictionary<string, object> data)
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
        public bool             loaded = false;
        public BonusDispatcher  bonusDispatcher = new BonusDispatcher ();

        public void Reset()
        {
            loaded = false;
            bonusDispatcher.reset();
        }

        public void Load(Dictionary<string, object> data)
        {
            loaded = true;
            var prefs = new JsonPrefs(data);
            bonusDispatcher.load(prefs.ValueObjectDict("BonusDispatcher"));
        }
    }

    public class BattleResult
    {
        public int                  place = 100;
        public bool                 isProperBattleEnd = false;
        public bool                 isEnoughPlayers = false;
        public ProfileInfo.Price    award = new ProfileInfo.Price(0, ProfileInfo.PriceCurrency.Silver);

        public bool                 isAllQuestsCompleted = false;
        public Quest                quest = null;
        public ProfileInfo.Price    questAll = new ProfileInfo.Price(0, ProfileInfo.PriceCurrency.Gold);

        public int                  silverBase = 0;
        public int                  silverVIP = 0;
        public int                  experienceBase = 0;
        public int                  experienceVIP = 0;
        public int                  fuel = 0;
        public int                  speedBoosters = 0;
        public int                  reloadBoosters = 0;
        public int                  attackBoosters = 0;
        public int                  heals = 0;
        public int                  mines = 0;
        public int                  missiles = 0;
        public int                  shoots = 0;
        public int                  shootsSaclos = 0;
        public int                  shootsIrcm = 0;
        public int                  hits = 0;
        public int                  hitsSaclos = 0;
        public int                  takenHits = 0;
        public int                  takenHitsSaclos = 0;
        public int                  givenDamage = 0;
        public int                  givenDamageSaclos = 0;
        public int                  takenDamage = 0;
        public int                  takenDamageSaclos = 0;
        public int                  frags = 0;
        public int                  deaths = 0;
        public float                mileage = 0f;
        public bool                 isBattleAsVip = false;
        public bool                 isDoubleExp = false;
        public bool                 isNewbie = false;
        public int                  maxKillsInARow = 0;

        private CurrencyValue[]     awards = null;

        public int Accuracy
        {
            get
            {
                return (int)(hits * 100f / shoots);
            }
        }

        public int AccuracySaclos
        {
            get
            {
                return (int)(hitsSaclos * 100f / shootsSaclos);
            }
        }

        public CurrencyValue[] GetAwardsFromList(List<object> list)
        {
            CurrencyValue[] result = new CurrencyValue[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                Dictionary<string, object> dict = (Dictionary<string, object>)list[i];
                result[i] = new CurrencyValue(dict.ExtractOrDefault<CurrencyType>("currency"), dict.ExtractOrDefault<int>("value"));
            }
            return result;
        }

        public void load(object dict)
        {
            JsonPrefs p = new JsonPrefs(dict);

            place = p.ValueInt("result/place");
            isProperBattleEnd = p.ValueBool("result/isProperBattleEnd");
            isEnoughPlayers = p.ValueBool("result/isEnoughPlayers");
            if (p.Contains("result/award"))
            {
                CurrencyValue[] awards = GetAwardsFromList(p.ValueObjectList("result/award"));
                StaticType.Awards.Instance<IAwards>().LastServerAward = awards;
                //award = ProfileInfo.Price.FromDictionary(p.ValueObjectDict("result/award"));
            }

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

            if (p.Contains("quest/current/id"))
            {
                quest = Quest.CreateFromDictionary(p.ValueObjectDict("quest/current"));
            }

            if (p.Contains("quest/all/value"))
            {
                questAll = ProfileInfo.Price.FromDictionary(p.ValueObjectDict("quest/all"));
            }
        }
    }

    public class BattleServer : MonoBehaviour, IBattleServer
    {
        public BattleOptions        options = new BattleOptions();
        public BotNames             botNames;
        public BattleResult         result = new BattleResult();
        public Quest                quest = null;

        protected XD.BattleResult   battleResult = XD.BattleResult.None;
        protected bool              isCaptured = false;
        protected int               battleId = -1;
        protected bool              isBattleBegin = false;
        protected Room              currentRoom = null;

        public string RoomName
        {
            get
            {
                if (currentRoom == null)
                {
                    return "null";
                }

                return currentRoom.Name;
            }
        }

        public int BattleID
        {
            get
            {
                return battleId;
            }

            set
            {
                battleId = value;
            }
        }

        #region IStatic
        public bool IsEmpty
        {
            get
            {
                return false;
            }
        }

        public StaticType StaticType
        {
            get
            {
                return StaticType.BattleServer;
            }
        }

        public void SaveInstance()
        {
            StaticContainer.Set(StaticType, this);
        }

        public void DeleteInstance()
        {
            StaticContainer.Set(StaticType, null);
        }
        #endregion

        #region ISender
        public string Description
        {
            get
            {
                return "[BattleServer] " + name;
            }

            set
            {
                name = value;
            }
        }

        private List<ISubscriber> subscribers = null;

        public List<ISubscriber> Subscribers
        {
            get
            {
                if (subscribers == null)
                {
                    subscribers = new List<ISubscriber>();
                }
                return subscribers;
            }
        }

        public void AddSubscriber(ISubscriber subscriber)
        {
            if (Subscribers.Contains(subscriber))
            {
                return;
            }
            Subscribers.Add(subscriber);
        }

        public void RemoveSubscriber(ISubscriber subscriber)
        {
            Subscribers.Remove(subscriber);
        }

        public void Event(Message message, params object[] parameters)
        {
            for (int i = 0; i < Subscribers.Count; i++)
            {
                Subscribers[i].Reaction(message, parameters);
            }
        }
        #endregion

        #region ISubscriber       
        public void Reaction(Message message, params object[] parameters)
        {
            switch (message)
            {
                case Message.PrepareToEndBattle:
                    battleResult = parameters.Get<XD.BattleResult>();
                    isCaptured = parameters.Get<bool>();
                    break;
            }
        }
        #endregion

        public bool IsWaitingForBattleEnd
        {
            get;
            private set;
        }

        private void Awake()
        {
            SaveInstance();
        }

        private void Start()
        {
            StaticType.GameController.AddSubscriber(this);
            IsWaitingForBattleEnd = false;
            Dispatcher.Subscribe(EventId.ItemTaken, OnItemTaken);
        }

        private void OnDestroy()
        {
            StaticType.GameController.RemoveSubscriber(this);
            Dispatcher.Unsubscribe(EventId.ItemTaken, OnItemTaken);

            DeleteInstance();
        }

        public void PrepareToBattle()
        {
            battleId = -1;
            options.Reset();
            result = new BattleResult();
            quest = null;
        }

        public void StartBattle(Room room, VehicleUpgrades vehicleUpgrades, XD.Settings vehicleParameters, bool isCreateRoom, bool roomWasFulled, Action<bool> result = null)
        {
            isBattleBegin = roomWasFulled;
            currentRoom = room;
            var data = new Dictionary<string, object>();
            data["room"] = new Dictionary<string, object>() {
                    {"name",        room.Name},
                    {"maxPlayers",  room.MaxPlayers},
                    {"isOpen",      room.IsOpen},
                    {"isVisible",   room.IsVisible},
                    {"summary",     room.ToStringFull ()}
                };

            data["map"] = Enum.Parse(typeof(MapId), room.CustomProperties["mp"].ToString()).ToString();
            data["mapId"] = room.CustomProperties["mp"];
            data["player"] = new Dictionary<string, object> {
                    {"teamId",          StaticContainer.GameManager.Team},
                    {"isVip",           ProfileInfo.IsPlayerVip},
                    {"isNewbie",        ProfileInfo.IsNewbie},
                    {"level",           StaticType.Profile.Instance<IProfile>().LevelCalculator.Level},
                    {"armor",           vehicleParameters[Setting.HP]},
                    {"damage",          vehicleParameters[Setting.Damage]},
                    {"rocketDamage",    vehicleParameters[Setting.Damage]},
                    {"rof",             vehicleParameters[Setting.RPM]},
                    {"ircmRof",         vehicleParameters[Setting.RPM]},
                    {"speed",           vehicleParameters[Setting.MovingSpeed]},
                };
            data["level"] = room.CustomProperties["lv"];
            data["mode"] = room.CustomProperties["gm"];
            data["punVersion"] = PhotonNetwork.versionPUN;
            data["protocolVersion"] = StaticContainer.GameManager.PhotonRoomVersion;
            data["isCreateRoom"] = isCreateRoom;

            data["timeStarted"] = isBattleBegin ? 0 : -1;

            DoRequest("/battle/start", data,
                // Success
                delegate (Response r)
                {
                    battleId = r.Prefs.ValueInt("battleId", -1);
                    if (r.Prefs.Contains("options"))
                    {
                        options.Load(r.Prefs.ValueObjectDict("options"));
                    }

                    botNames = new BotNames(r.Prefs);
                    if (r.Prefs.Contains("quest"))
                    {
                        quest = Quest.CreateFromDictionary(r.Prefs.ValueObjectDict("quest"));

                    }

                    if (result != null)
                    {
                        result(true);
                    }
                },
                // Fail
                delegate (Response r)
                {
                    if (result != null)
                    {
                        result(false);
                    }
                }
            );
        }

        public void EndBattle(string disconnectCause, Action<bool> finished = null)
        {
            if (battleId < 0)
            {
                return;
            }

            var data = new Dictionary<string, object> {
                {"battleResult", (int)battleResult },
                {"isCaptured", isCaptured },
                {"place", StaticContainer.BattleController.Rank},
                {"isProperBattleEnd", BattleStatisticsManager.BattleStats["ProperEndBattle"]},
                {"isPhotonDisconnect", BattleStatisticsManager.BattleStats["PhotonDisconnect"]},
                {"isEnoughPlayers", StaticContainer.BattleController.IsEnoughPlayers},
                {"disconnectCause", disconnectCause },
                {"events", StaticType.BattleEventQueue.Instance<IBattleEventQueue>().GetDataEvents(true) }
            };

            IsWaitingForBattleEnd = true;
            DoRequest("/battle/end", data,
                (Response r) =>
                { // Success
                    result.load(r.Data);
                    IsWaitingForBattleEnd = false;
                    if (finished != null)
                    {
                        finished(true);
                    }
                },
                (Response r) =>
                { // Fail
                    IsWaitingForBattleEnd = false;
                    Debug.LogError("Battle result load failed!");
                    if (finished != null)
                    {
                        finished(false);
                    }
                }
            );
        }

        /// <summary>
        /// Оповещение сервера о запуске таймера боя
        /// </summary>
        /// <param name="secondsFromStart">На какой секунде от старта боя запущен таймер</param>
        public void StartTimer(int secondsFromStart)
        {
            if (isBattleBegin)
                return;
            isBattleBegin = true;
            DoRequest("/battle/begin", new Dictionary<string, object>() {
                {"timeStarted", secondsFromStart},
            });
        }
        
        public void BuyRespawnBonus(BonusItem.BonusType type, int gold, Request.WWWResultCallback success = null, Request.WWWResultCallback fail = null)
        {
            var price = new ProfileInfo.Price(gold, ProfileInfo.PriceCurrency.Gold);
            DoRequest("/battle/buyBonus", new Dictionary<string, object>(){
                {"type", type},
                {"price", price.ToDictionary ()}
            }, success, fail);
        }

        public void BuyGameProlongation(int gold, Request.WWWResultCallback success = null, Request.WWWResultCallback fail = null)
        {
            var price = new ProfileInfo.Price(gold, ProfileInfo.PriceCurrency.Gold);
            DoRequest("/battle/buyProlongation", new Dictionary<string, object>() {
                {"duration", GameData.ProlongTimeAddition},
                {"price", price.ToDictionary ()}
            }, success, fail);
        }

        public void BuyHastenRespawn(Action<bool> result = null)
        {
            DoRequest("/battle/buyHastenRespawn", new Dictionary<string, object>() { },
                delegate (Response r)
                {
                    if (result != null)
                    {
                        result(true);
                    }
                }, delegate (Response r)
                {
                    if (result != null)
                    {
                        result(false);
                    }
                }
            );
        }

        private void OnItemTaken(EventId id, EventInfo ei)
        {
            if (!StaticContainer.Profile.BattleTutorialCompleted)
            {
                return;
            }

            EventInfo_III info = (EventInfo_III)ei;

            var bonusType = (BonusItem.BonusType)info.int1;
            var amount = info.int2;

            if (info.int3 != StaticType.BattleController.Instance<IBattleController>().MyPlayerId)
            {
                return;
            }

            DoRequest("/battle/takeItem", new Dictionary<string, object>() {
                    {"item", bonusType.ToString ()},
                    {"amount", amount}
                },
                (Response r) =>
                { // Success
                    if (r.Prefs.Contains("quest"))
                    {
                        CheckQuestUpdate(Quest.CreateFromDictionary(r.Prefs.ValueObjectDict("quest")));
                    }
                }
            );
        }

        private void DoRequest(string path, Dictionary<string, object> data, Request.WWWResultCallback success = null, Request.WWWResultCallback fail = null)
        {
            var stats = new Dictionary<string, object>();
            if (BattleController.Instance && StaticContainer.BattleController.CurrentUnit)
            {
                stats["odometer"] = StaticContainer.BattleController.CurrentUnit.Odometer;
            }
            else
            {
                stats["odometer"] = BattleStatisticsManager.BattleStats["Mileage"];
            }
            stats["shoots"] = BattleStatisticsManager.BattleStats["Shoots"];
            stats["shootsIrcm"] = BattleStatisticsManager.BattleStats["Shoots_IRCM"];
            stats["hits"] = BattleStatisticsManager.BattleStats["Hits"];
            stats["takenHits"] = BattleStatisticsManager.BattleStats["TakenHits"];
            stats["givenDamage"] = BattleStatisticsManager.BattleStats["GivenDamage"];
            stats["takenDamage"] = BattleStatisticsManager.BattleStats["TakenDamage"];
            data["stats"] = stats;
            data["ping"] = PhotonNetwork.GetPing();

            var request = Manager.Instance().CreateRequest(path);
            request.Form.AddField("roomName", RoomName);
            request.Form.AddField("timestamp", ((long)GameData.CurrentTimeStamp).ToString(CultureInfo.InvariantCulture));
            request.Form.AddField("timeInBattle", ((int)BattleController.TimeInBattleUnity).ToString(CultureInfo.InvariantCulture));
            request.Form.AddField("timeInBattlePhoton", ((int)BattleController.TimeInBattlePhoton).ToString(CultureInfo.InvariantCulture));
            if (battleId > 0)
            {
                request.Form.AddField("battleId", battleId.ToString(CultureInfo.InvariantCulture));
            }

            request.Form.AddField("json", MiniJSON.Json.Serialize(data));
            Manager.StartAsyncRequest(request, success, fail);
        }

        private void CheckQuestUpdate(Quest q)
        {
            if (quest == null)
            {
                return;
            }

            if ((q.progress != quest.progress) || (q.isComplete != quest.isComplete))
            {
                if (q.type != Quest.Type.Mileage)
                {
                    bool isCompleted = q.isComplete && !quest.isComplete;
                    quest = q;
                    Dispatcher.Send(EventId.BattleQuestUpdated, new EventInfo_SimpleEvent());
                    if (isCompleted)
                    {
                        Dispatcher.Send(EventId.QuestCompleted, new EventInfo_I((int)quest.type));
                    }
                }
            }
        }
    }
}