using System;
using System.Collections.Generic;
using System.Linq;
using Tanks.Models;
using UnityEngine;
using JSONObject = System.Collections.Generic.Dictionary<string, object>;

namespace XDevs.Scores.Animation
{
    public class ScoresAnimationManager : MonoBehaviour, IQueueablePage
    {
        [SerializeField] private ScoresAnimationController windowsPrefab;
        [SerializeField] private Transform windowsAnchorPoint;
        [SerializeField] private bool debug;

        public static bool Dbg
        {
            get { return Instance != null && Instance.debug; }
        }

        // In case of severe OCD break glass (also uncomment overloaded == operator)
        //[SerializeField] 
        private LeaderboardDelta deltaToShow;

        private ScoresAnimationController scoresAnimationController;
        
        private bool safeToSaveOldLeaderboardItems;

        public static ScoresAnimationManager Instance { get; private set; }

        private static Dictionary<string, Dictionary<string, LeaderboardItem>> oldLeaderboardItemByAreaByTime;
        private static LeaderboardItem oldFriendsLeaderboardItem;

        private bool scoresHighlightedItemsReady;
        private bool friendsScoresHighlightedItemReady;

        private void Awake()
        {
            Instance = this;
            Dispatcher.Subscribe(EventId.ScoresHighlightedItemsReady, OnScoresHighlightedItemsReady);
            Dispatcher.Subscribe(EventId.FriendsScoresHighlightedItemsReady, OnFriendsScoresHighlightedItemReady);
        }

        private void OnDestroy()
        {
            Dispatcher.Unsubscribe(EventId.ScoresHighlightedItemsReady, OnScoresHighlightedItemsReady);
            Dispatcher.Unsubscribe(EventId.FriendsScoresHighlightedItemsReady, OnFriendsScoresHighlightedItemReady);
            Instance = null;
        }

        private bool CheckScoresDataForDelta(LeaderboardDelta delta, JSONObject scoresData, List<object> friendsScoresData)
        {
            switch (delta.animation)
            {
                case Animation.ChangePlace:

                    List<object> leaderboard;

                    if (delta.area == "friends")
                    {
                        leaderboard = friendsScoresData;
                    }
                    else
                    {
                        var areaDict = new JsonPrefs(scoresData[delta.time]).ValueObjectDict(delta.area);
                        var areaPrefs = new JsonPrefs(areaDict);
                        leaderboard = areaPrefs.ValueObjectList("leaderBoard");
                    }

                    if (leaderboard.Count < 2)
                    {
                        if (debug)
                            Debug.LogErrorFormat("ScoresAnimationManager.CheckScoresDataForDelta({0}) == false", delta);

                        return false;
                    }

                    break;

                case Animation.TransitionNumbers:
                    break;
            }

            if (debug)
                Debug.LogErrorFormat("ScoresAnimationManager.CheckScoresDataForDelta({0}) == true", delta);

            return true;
        }

        public void BeforeActivation() { }

        public void Activated()
        {
            if (debug)
                Debug.LogError("ScoresAnimationManager.Activated()");

            if (deltaToShow != null)
            {
                Init();

                if (debug)
                    Debug.LogError("deltaToShow: " + deltaToShow);

                Show();

                return;
            }

            if (debug)
                Debug.LogError("Got no deltas :(");
        }

        private void SaveOldLeaderboardItemByAreaByTime()
        {
            if (ScoresController.Instance == null)
                return;

            oldLeaderboardItemByAreaByTime = new Dictionary<string, Dictionary<string, LeaderboardItem>>();

            if (ScoresController.Instance.HighlightedItemByAreaByTime != null)
            {
                foreach (var time in ScoresController.Instance.HighlightedItemByAreaByTime)
                {
                    if (time.Value == null)
                        continue;

                    foreach (var area in time.Value)
                    {
                        if (area.Value == null)
                            continue;

                        if (!oldLeaderboardItemByAreaByTime.ContainsKey(time.Key))
                            oldLeaderboardItemByAreaByTime[time.Key] = new Dictionary<string, LeaderboardItem>();

                        oldLeaderboardItemByAreaByTime[time.Key][area.Key] =
                            new LeaderboardItem(area.Value.Place, ((ICanHazScore) area.Value).Score ?? 0);

                        if (debug)
                            Debug.LogErrorFormat("oldLeaderboardItemByAreaByTime[\"{0}\"][\"{1}\"]: {2}", time.Key,
                                area.Key, oldLeaderboardItemByAreaByTime[time.Key][area.Key]);
                    }
                }
            }
        }

        private void SaveOldFriendsLeaderboardItem()
        {
            if (ScoresController.Instance == null)
                return;

            if (ScoresController.Instance.friendsScores.HighlightedItem != null)
            {
                oldFriendsLeaderboardItem =
                    new LeaderboardItem(
                        ScoresController.Instance.friendsScores.HighlightedItem.Place,
                        ((ICanHazScore)ScoresController.Instance.friendsScores.HighlightedItem).Score ?? 0);

                if (debug)
                    Debug.LogErrorFormat("oldFriendsLeaderboardItem: {0}", oldFriendsLeaderboardItem);
            }
        }

        private void SaveOldLeaderboardItems()
        {
            if (debug)
                Debug.LogError("ScoresAnimationManager.SaveOldLeaderboardItems()");

            SaveOldLeaderboardItemByAreaByTime();
            SaveOldFriendsLeaderboardItem();
        }

        private void CheckBothReady()
        {
            if (scoresHighlightedItemsReady && friendsScoresHighlightedItemReady)
            {
                if (ScoresController.Instance != null)
                {
                    var deltas = LeaderboardDeltas();

                    SaveOldLeaderboardItems();

                    if (deltas != null)
                    {
                        foreach (
                            var deltaToCheckBounds in
                                GetDeltasToShow(deltas, ScoresController.ScoresData, FriendsManager.FriendsScoresData))
                        {
                            if (
                                !CheckScoresDataForDelta(deltaToCheckBounds, ScoresController.ScoresData,
                                    FriendsManager.FriendsScoresData))
                                continue;

                            deltaToShow = deltaToCheckBounds;

                            safeToSaveOldLeaderboardItems = true;
                            
                            if (BattleStatisticsManager.BattleStats["ConnectionFailed"] != 1)
                            {
                                var voiceEventId =
                                    (int)
                                        (ProfileInfo.IsPlayerVip
                                            ? VoiceEventKey.LeaderBoardWindowVIP
                                            : VoiceEventKey.LeaderBoardWindow);

                                GUIPager.EnqueuePage("ScoresAnimation", true, true, voiceEventId);
                            }
                            
                            break;
                        }
                    }
                }
            }
        }

        private void OnScoresHighlightedItemsReady(EventId id, EventInfo info)
        {
            scoresHighlightedItemsReady = true;

            if (debug)
            {
                Debug.LogError("ScoresAnimationManager.OnScoresHighlightedItemsReady()");
                Debug.LogError("scoresHighlightedItemsReady: " + scoresHighlightedItemsReady);
                Debug.LogError("friendsScoresHighlightedItemReady: " + friendsScoresHighlightedItemReady);
                Debug.LogError("HangarController.FirstEnter: " + HangarController.FirstEnter);
                Debug.LogError("safeToSaveOldLeaderboardItems: " + safeToSaveOldLeaderboardItems);
            }

            if (HangarController.FirstEnter || safeToSaveOldLeaderboardItems)
            {
                SaveOldLeaderboardItemByAreaByTime();
                return;
            }

            CheckBothReady();
        }

        private void OnFriendsScoresHighlightedItemReady(EventId id, EventInfo info)
        {
            friendsScoresHighlightedItemReady = true;

            if (debug)
            {
                Debug.LogError("ScoresAnimationManager.OnFriendsScoresHighlightedItemReady()");
                Debug.LogError("scoresHighlightedItemsReady: " + scoresHighlightedItemsReady);
                Debug.LogError("friendsScoresHighlightedItemReady: " + friendsScoresHighlightedItemReady);
                Debug.LogError("HangarController.FirstEnter: " + HangarController.FirstEnter);
                Debug.LogError("safeToSaveOldLeaderboardItems: " + safeToSaveOldLeaderboardItems);
            }

            if (HangarController.FirstEnter || safeToSaveOldLeaderboardItems)
            {
                SaveOldFriendsLeaderboardItem();
                return;
            }

            CheckBothReady();
        }

        private void Init()
        {
            if (scoresAnimationController == null)
            {
                scoresAnimationController = Instantiate(windowsPrefab);

                scoresAnimationController.gameObject.SetActive(false);

                var pos = scoresAnimationController.transform.localPosition; // saving before parent's change
                scoresAnimationController.transform.parent = windowsAnchorPoint;
                scoresAnimationController.transform.localPosition = pos; // restoring after parent's change
            }

            scoresAnimationController.StopAllCoroutines();

            if (debug)
                Debug.LogError("ScoresAnimationManager.Init() done");
        }

        private void Show()
        {
            if (debug)
                Debug.LogError("ScoresAnimationManager.Show();");

            if (!scoresAnimationController.Init(ScoresController.ScoresData, FriendsManager.FriendsScoresData, deltaToShow))
            {
                GUIPager.SetActivePage("MainMenu");
                return;
            }

            scoresAnimationController.gameObject.SetActive(true);
            scoresAnimationController.Show();
        }

        /*
        1) Максимальная дельта места не подходит
        2) Приоритет красивой анимации
        3) Если есть кандидаты для красивой анимации — показываем максимальную дельту из них
        4) Если нет — показываем анимацию для дельты с максимальной относительной дельтой, placeDelta/oldPlace
        */
        private List<LeaderboardDelta> GetDeltasToShow(List<LeaderboardDelta> deltas, JSONObject scoresData, List<object> friendsScoresData)
        {
            if (deltas == null || deltas.Count == 0)
                return null;

            var deltasToShow = new List<LeaderboardDelta>();

            var placeChangeDeltas = new Dictionary<float, LeaderboardDelta>();
            var numbersTransitionDeltas = new Dictionary<float, LeaderboardDelta>();

            foreach (var delta in deltas)
            {
                if (delta.PlaceDelta < 1)
                    continue;

                List<object> leaderboard;

                if (delta.area == "friends")
                {
                    leaderboard = friendsScoresData;
                }
                else
                {
                    var areaDict = new JsonPrefs(scoresData[delta.time]).ValueObjectDict(delta.area);
                    var areaPrefs = new JsonPrefs(areaDict);
                    leaderboard = areaPrefs.ValueObjectList("leaderBoard");
                }
                
                int scoresItemToMovePosition;

                switch (delta.area)
                {
                    case "clans":
                        if (ProfileInfo.Clan == null)
                            continue;

                        var leaderboardClans = new List<Clan>();

                        foreach (var clanJSONObject in leaderboard)
                        {
                            var clanPrefs = new JsonPrefs(clanJSONObject);
                            leaderboardClans.Add(Clan.Create(clanPrefs));
                        }

                        scoresItemToMovePosition =
                            leaderboardClans.IndexOf(
                                leaderboardClans.Find(clanItem => clanItem.Id == ProfileInfo.Clan.Id));
                        break;

                    default:
                        var leaderboardPlayers = new List<Player>();

                        foreach (var playerJSONObject in leaderboard)
                        {
                            var playerPrefs = new JsonPrefs(playerJSONObject);
                            leaderboardPlayers.Add(Player.Create(playerPrefs));
                        }

                        scoresItemToMovePosition =
                            leaderboardPlayers.IndexOf(
                                leaderboardPlayers.Find(playerItem => playerItem.Id == ProfileInfo.profileId));
                        break;
                }

                var relativeDelta = (float)delta.PlaceDelta / (float)delta.oldLeaderboardItem.place;

                if (scoresItemToMovePosition == leaderboard.Count - 1)
                {
                    numbersTransitionDeltas[relativeDelta] =
                        new LeaderboardDelta(
                            time: delta.time,
                            area: delta.area,
                            oldLeaderboardItem: delta.oldLeaderboardItem,
                            newLeaderboardItem: delta.newLeaderboardItem,
                            animation: Animation.TransitionNumbers);

                    if (debug)
                        Debug.LogErrorFormat("GetDeltasToShow.numbersTransitionDeltas.LeaderboardDelta: {0}, {1}", relativeDelta, delta);
                }
                else
                {
                    placeChangeDeltas[relativeDelta] =
                        new LeaderboardDelta(
                            time: delta.time,
                            area: delta.area,
                            oldLeaderboardItem: delta.oldLeaderboardItem,
                            newLeaderboardItem: delta.newLeaderboardItem,
                            animation: Animation.ChangePlace);

                    if (debug)
                        Debug.LogErrorFormat("GetDeltasToShow.placeChangeDeltas.LeaderboardDelta: {0}, {1}", relativeDelta, delta);
                }
            }

            deltasToShow.AddRange(
                placeChangeDeltas
                    .OrderByDescending(placeChangeDelta => placeChangeDelta.Key)
                    .Select(placeChangeDelta => placeChangeDelta.Value));

            deltasToShow.AddRange(
                numbersTransitionDeltas
                    .OrderByDescending(numbersTransitionDelta => numbersTransitionDelta.Key)
                    .Select(numbersTransitionDelta => numbersTransitionDelta.Value));

            string keyTime;
            string keyPlace;

            ScoresController.Instance.GetPreferredScoresPage(out keyTime, out keyPlace);

            var preferredDelta = deltasToShow.Find(
                delta => delta.time == keyTime && delta.area == keyPlace);

            if (preferredDelta != null)
            {
                if (debug)
                    Debug.LogErrorFormat("GetDeltasToShow.preferredDelta: {0}, переносим в начало списка дельт", preferredDelta);

                deltasToShow.Remove(preferredDelta);
                deltasToShow.Insert(0, preferredDelta);
            }

            if (debug)
            {
                foreach (var delta in deltasToShow)
                {
                    Debug.LogErrorFormat("GetDeltasToShow.deltaToShow: {0}", delta);
                }
            }

            return deltasToShow;
        }

        private List<LeaderboardDelta> LeaderboardDeltas()
        {
            if (ScoresController.Instance.HighlightedItemByAreaByTime == null)
                return null;

            var deltas = new List<LeaderboardDelta>();

            #region Test mockup
            //Смотри также Test mockup в ScoresAnimation.cs
            //deltas.Clear();

            //deltas.Add(new LeaderboardDelta("allTime", "world",
            //    new LeaderboardItem(9, -500), new LeaderboardItem(1, 100001)));
            //deltas.Add(new LeaderboardDelta("allTime", "country",
            //    new LeaderboardItem(65536, -500), new LeaderboardItem(42, 100001)));
            //deltas.Add(new LeaderboardDelta("allTime", "world",
            //    new LeaderboardItem(10, -500), new LeaderboardItem(2, 100001)));

            //return deltas;
            #endregion

            if (oldLeaderboardItemByAreaByTime == null)
                return null;

            foreach (var time in ScoresController.Instance.HighlightedItemByAreaByTime)
            {
                if (!oldLeaderboardItemByAreaByTime.ContainsKey(time.Key)
                    || oldLeaderboardItemByAreaByTime[time.Key] == null)
                    continue;

                foreach (var area in time.Value)
                {
                    if (area.Value != null && oldLeaderboardItemByAreaByTime[time.Key].ContainsKey(area.Key))
                    {
                        var delta = oldLeaderboardItemByAreaByTime[time.Key][area.Key].place - area.Value.Place;

                        if (delta > 0)
                        {
                            deltas.Add(new LeaderboardDelta(
                                time: time.Key,
                                area: area.Key,
                                oldLeaderboardItem: oldLeaderboardItemByAreaByTime[time.Key][area.Key],
                                newLeaderboardItem:
                                    new LeaderboardItem(area.Value.Place, ((ICanHazScore)area.Value).Score ?? 0)));
                        }
                    }
                }
            }

            if (oldFriendsLeaderboardItem != null
                && ScoresController.Instance.friendsScores != null
                && ScoresController.Instance.friendsScores.HighlightedItem != null)
            {
                var delta = oldFriendsLeaderboardItem.place - ScoresController.Instance.friendsScores.HighlightedItem.Place;

                if (delta > 0)
                {
                    deltas.Add(new LeaderboardDelta(
                        time: null,
                        area: "friends",
                        oldLeaderboardItem: oldFriendsLeaderboardItem,
                        newLeaderboardItem:
                            new LeaderboardItem(
                                ScoresController.Instance.friendsScores.HighlightedItem.Place, 
                                ((ICanHazScore)ScoresController.Instance.friendsScores.HighlightedItem).Score ?? 0)));
                }
            }

            return deltas.Count == 0 ? null : deltas;
        }

        public void TestScoresAnimation()
        {
            // ScoresManager.Instance.SaveScoresToServer();

            #region Тестовые данные

            var statsMockup =
                @"{
  ""stats"": {
    ""allTime"": {
      ""world"": {
        ""leaderBoard"": [
          {
            ""id"": 137,
            ""nickName"": ""Guest218"",
            ""lastActiveTime"": 1474173829,
            ""countryCode"": ""RU"",
            ""place"": 1,
            ""score"": 59500200
          },
          {
            ""id"": 82,
            ""nickName"": ""Guest702"",
            ""lastActiveTime"": 1474193826,
            ""countryCode"": ""RU"",
            ""place"": 2,
            ""score"": 50081695
          },
          {
            ""id"": 63,
            ""place"": 3,
            ""score"": 50010455
          },
          {
            ""id"": 29,
            ""nickName"": ""Guest127"",
            ""lastActiveTime"": 1472192889,
            ""countryCode"": ""RU"",
            ""place"": 4,
            ""score"": 50000000
          },
          {
            ""id"": 142,
            ""nickName"": ""Guest686"",
            ""lastActiveTime"": 1474178860,
            ""countryCode"": ""RU"",
            ""place"": 5,
            ""score"": 50000000
          },
          {
            ""id"": 55,
            ""nickName"": ""Guest265"",
            ""lastActiveTime"": 1473417617,
            ""countryCode"": ""RU"",
            ""place"": 6,
            ""score"": 36250900
          },
          {
            ""id"": 105,
            ""nickName"": ""Guest153"",
            ""lastActiveTime"": 1474026565,
            ""countryCode"": ""RU"",
            ""place"": 7,
            ""score"": 22250795
          },
          {
            ""id"": 78,
            ""nickName"": ""Guest539"",
            ""lastActiveTime"": 1473938997,
            ""countryCode"": ""RU"",
            ""place"": 8,
            ""score"": 16264430
          },
          {
            ""id"": 132,
            ""nickName"": ""Demetri"",
            ""lastActiveTime"": 1474183588,
            ""countryCode"": ""RU"",
            ""place"": 9,
            ""score"": 10000260
          },
          {
            ""id"": 129,
            ""nickName"": ""nika"",
            ""lastActiveTime"": 1474104364,
            ""countryCode"": ""RU"",
            ""place"": 10,
            ""score"": 8800450
          },
          {
            ""id"": 99,
            ""nickName"": ""Nika"",
            ""lastActiveTime"": 1473938030,
            ""countryCode"": ""RU"",
            ""place"": 11,
            ""score"": 8377005
          },
          {
            ""id"": 83,
            ""nickName"": ""Guest512"",
            ""lastActiveTime"": 1473921159,
            ""countryCode"": ""RU"",
            ""place"": 12,
            ""score"": 5013965
          },
          {
            ""id"": 146,
            ""nickName"": ""Demetri"",
            ""lastActiveTime"": 1474196148,
            ""countryCode"": ""RU"",
            ""place"": 13,
            ""score"": 5000135
          },
          {
            ""id"": 93,
            ""nickName"": ""qwe123"",
            ""lastActiveTime"": 1473943233,
            ""countryCode"": ""RU"",
            ""place"": 14,
            ""score"": 947440
          },
          {
            ""id"": 11,
            ""nickName"": ""mr green"",
            ""lastActiveTime"": 1473771844,
            ""countryCode"": ""RU"",
            ""place"": 15,
            ""score"": 818015
          },
          {
            ""id"": 25,
            ""nickName"": ""Guest82"",
            ""lastActiveTime"": 1474187920,
            ""countryCode"": ""RU"",
            ""vkontakte"": ""271527343"",
            ""place"": 16,
            ""score"": 773790
          },
          {
            ""id"": 32,
            ""nickName"": ""Guest567"",
            ""lastActiveTime"": 1473947241,
            ""countryCode"": ""RU"",
            ""place"": 17,
            ""score"": 750690
          },
          {
            ""id"": 89,
            ""nickName"": ""Guest922"",
            ""lastActiveTime"": 1473826834,
            ""countryCode"": ""RU"",
            ""place"": 18,
            ""score"": 537020
          },
          {
            ""id"": 45,
            ""nickName"": ""Guest557"",
            ""lastActiveTime"": 1473324331,
            ""countryCode"": ""RU"",
            ""place"": 19,
            ""score"": 533690
          },
          {
            ""id"": 110,
            ""nickName"": ""Demetri"",
            ""lastActiveTime"": 1474028228,
            ""countryCode"": ""RU"",
            ""place"": 20,
            ""score"": 520145
          },
          {
            ""id"": 13,
            ""nickName"": ""Guest79"",
            ""lastActiveTime"": 1467957327,
            ""countryCode"": ""RU"",
            ""place"": 70,
            ""score"": 500
          },
          {
            ""id"": 46,
            ""nickName"": ""Guest74"",
            ""lastActiveTime"": 1473080112,
            ""countryCode"": ""RU"",
            ""place"": 71,
            ""score"": 500
          },
          {
            ""id"": 131,
            ""nickName"": ""Хер Майор"",
            ""lastActiveTime"": 1474029307,
            ""countryCode"": ""RU"",
            ""place"": 72,
            ""score"": 500
          },
          {
            ""id"": 128,
            ""nickName"": ""kirito"",
            ""lastActiveTime"": 1474028957,
            ""countryCode"": ""RU"",
            ""place"": 73,
            ""score"": 500
          },
          {
            ""id"": 139,
            ""nickName"": ""Guest829"",
            ""lastActiveTime"": 1474192012,
            ""countryCode"": ""RU"",
            ""place"": 74,
            ""score"": 500
          },
          {
            ""id"": 145,
            ""nickName"": ""Guest372"",
            ""lastActiveTime"": 1474198109,
            ""countryCode"": ""RU"",
            ""place"": 75,
            ""score"": 500
          }
        ]
      },
      ""country"": {
        ""name"": ""Russian Federation"",
        ""code"": ""RU"",
        ""leaderBoard"": [
          {
            ""id"": 137,
            ""nickName"": ""Guest218"",
            ""lastActiveTime"": 1474173829,
            ""countryCode"": ""RU"",
            ""place"": 1,
            ""score"": 59500200
          },
          {
            ""id"": 82,
            ""nickName"": ""Guest702"",
            ""lastActiveTime"": 1474193826,
            ""countryCode"": ""RU"",
            ""place"": 2,
            ""score"": 50081695
          },
          {
            ""id"": 63,
            ""place"": 3,
            ""score"": 50010455
          },
          {
            ""id"": 29,
            ""nickName"": ""Guest127"",
            ""lastActiveTime"": 1472192889,
            ""countryCode"": ""RU"",
            ""place"": 4,
            ""score"": 50000000
          },
          {
            ""id"": 142,
            ""nickName"": ""Guest686"",
            ""lastActiveTime"": 1474178860,
            ""countryCode"": ""RU"",
            ""place"": 5,
            ""score"": 50000000
          },
          {
            ""id"": 55,
            ""nickName"": ""Guest265"",
            ""lastActiveTime"": 1473417617,
            ""countryCode"": ""RU"",
            ""place"": 6,
            ""score"": 36250900
          },
          {
            ""id"": 105,
            ""nickName"": ""Guest153"",
            ""lastActiveTime"": 1474026565,
            ""countryCode"": ""RU"",
            ""place"": 7,
            ""score"": 22250795
          },
          {
            ""id"": 78,
            ""nickName"": ""Guest539"",
            ""lastActiveTime"": 1473938997,
            ""countryCode"": ""RU"",
            ""place"": 8,
            ""score"": 16264430
          },
          {
            ""id"": 132,
            ""nickName"": ""Demetri"",
            ""lastActiveTime"": 1474183588,
            ""countryCode"": ""RU"",
            ""place"": 9,
            ""score"": 10000260
          },
          {
            ""id"": 129,
            ""nickName"": ""nika"",
            ""lastActiveTime"": 1474104364,
            ""countryCode"": ""RU"",
            ""place"": 10,
            ""score"": 8800450
          },
          {
            ""id"": 99,
            ""nickName"": ""Nika"",
            ""lastActiveTime"": 1473938030,
            ""countryCode"": ""RU"",
            ""place"": 11,
            ""score"": 8377005
          },
          {
            ""id"": 83,
            ""nickName"": ""Guest512"",
            ""lastActiveTime"": 1473921159,
            ""countryCode"": ""RU"",
            ""place"": 12,
            ""score"": 5013965
          },
          {
            ""id"": 146,
            ""nickName"": ""Demetri"",
            ""lastActiveTime"": 1474196148,
            ""countryCode"": ""RU"",
            ""place"": 13,
            ""score"": 5000135
          },
          {
            ""id"": 93,
            ""nickName"": ""qwe123"",
            ""lastActiveTime"": 1473943233,
            ""countryCode"": ""RU"",
            ""place"": 14,
            ""score"": 947440
          },
          {
            ""id"": 11,
            ""nickName"": ""mr green"",
            ""lastActiveTime"": 1473771844,
            ""countryCode"": ""RU"",
            ""place"": 15,
            ""score"": 818015
          },
          {
            ""id"": 25,
            ""nickName"": ""Guest82"",
            ""lastActiveTime"": 1474187920,
            ""countryCode"": ""RU"",
            ""vkontakte"": ""271527343"",
            ""place"": 16,
            ""score"": 773790
          },
          {
            ""id"": 32,
            ""nickName"": ""Guest567"",
            ""lastActiveTime"": 1473947241,
            ""countryCode"": ""RU"",
            ""place"": 17,
            ""score"": 750690
          },
          {
            ""id"": 89,
            ""nickName"": ""Guest922"",
            ""lastActiveTime"": 1473826834,
            ""countryCode"": ""RU"",
            ""place"": 18,
            ""score"": 537020
          },
          {
            ""id"": 45,
            ""nickName"": ""Guest557"",
            ""lastActiveTime"": 1473324331,
            ""countryCode"": ""RU"",
            ""place"": 19,
            ""score"": 533690
          },
          {
            ""id"": 110,
            ""nickName"": ""Demetri"",
            ""lastActiveTime"": 1474028228,
            ""countryCode"": ""RU"",
            ""place"": 20,
            ""score"": 520145
          },
          {
            ""id"": 7,
            ""nickName"": ""Guest55"",
            ""lastActiveTime"": 1467373516,
            ""countryCode"": ""RU"",
            ""place"": 70,
            ""score"": 500
          },
          {
            ""id"": 1,
            ""nickName"": ""Guest672"",
            ""lastActiveTime"": 1469439804,
            ""countryCode"": ""RU"",
            ""place"": 71,
            ""score"": 500
          },
          {
            ""id"": 131,
            ""nickName"": ""Хер Майор"",
            ""lastActiveTime"": 1474029307,
            ""countryCode"": ""RU"",
            ""place"": 72,
            ""score"": 500
          },
          {
            ""id"": 128,
            ""nickName"": ""kirito"",
            ""lastActiveTime"": 1474028957,
            ""countryCode"": ""RU"",
            ""place"": 73,
            ""score"": 500
          },
          {
            ""id"": 139,
            ""nickName"": ""Guest829"",
            ""lastActiveTime"": 1474192012,
            ""countryCode"": ""RU"",
            ""place"": 74,
            ""score"": 500
          },
          {
            ""id"": 145,
            ""nickName"": ""Guest372"",
            ""lastActiveTime"": 1474198109,
            ""countryCode"": ""RU"",
            ""place"": 75,
            ""score"": 500
          }
        ]
      },
      ""region"": {
        ""name"": ""Chelyabinsk"",
        ""leaderBoard"": [
          {
            ""id"": 137,
            ""nickName"": ""Guest218"",
            ""lastActiveTime"": 1474173829,
            ""countryCode"": ""RU"",
            ""place"": 1,
            ""score"": 59500200
          },
          {
            ""id"": 82,
            ""nickName"": ""Guest702"",
            ""lastActiveTime"": 1474193826,
            ""countryCode"": ""RU"",
            ""place"": 2,
            ""score"": 50081695
          },
          {
            ""id"": 63,
            ""place"": 3,
            ""score"": 50010455
          },
          {
            ""id"": 29,
            ""nickName"": ""Guest127"",
            ""lastActiveTime"": 1472192889,
            ""countryCode"": ""RU"",
            ""place"": 4,
            ""score"": 50000000
          },
          {
            ""id"": 142,
            ""nickName"": ""Guest686"",
            ""lastActiveTime"": 1474178860,
            ""countryCode"": ""RU"",
            ""place"": 5,
            ""score"": 50000000
          },
          {
            ""id"": 55,
            ""nickName"": ""Guest265"",
            ""lastActiveTime"": 1473417617,
            ""countryCode"": ""RU"",
            ""place"": 6,
            ""score"": 36250900
          },
          {
            ""id"": 105,
            ""nickName"": ""Guest153"",
            ""lastActiveTime"": 1474026565,
            ""countryCode"": ""RU"",
            ""place"": 7,
            ""score"": 22250795
          },
          {
            ""id"": 78,
            ""nickName"": ""Guest539"",
            ""lastActiveTime"": 1473938997,
            ""countryCode"": ""RU"",
            ""place"": 8,
            ""score"": 16264430
          },
          {
            ""id"": 132,
            ""nickName"": ""Demetri"",
            ""lastActiveTime"": 1474183588,
            ""countryCode"": ""RU"",
            ""place"": 9,
            ""score"": 10000260
          },
          {
            ""id"": 129,
            ""nickName"": ""nika"",
            ""lastActiveTime"": 1474104364,
            ""countryCode"": ""RU"",
            ""place"": 10,
            ""score"": 8800450
          },
          {
            ""id"": 99,
            ""nickName"": ""Nika"",
            ""lastActiveTime"": 1473938030,
            ""countryCode"": ""RU"",
            ""place"": 11,
            ""score"": 8377005
          },
          {
            ""id"": 83,
            ""nickName"": ""Guest512"",
            ""lastActiveTime"": 1473921159,
            ""countryCode"": ""RU"",
            ""place"": 12,
            ""score"": 5013965
          },
          {
            ""id"": 146,
            ""nickName"": ""Demetri"",
            ""lastActiveTime"": 1474196148,
            ""countryCode"": ""RU"",
            ""place"": 13,
            ""score"": 5000135
          },
          {
            ""id"": 93,
            ""nickName"": ""qwe123"",
            ""lastActiveTime"": 1473943233,
            ""countryCode"": ""RU"",
            ""place"": 14,
            ""score"": 947440
          },
          {
            ""id"": 11,
            ""nickName"": ""mr green"",
            ""lastActiveTime"": 1473771844,
            ""countryCode"": ""RU"",
            ""place"": 15,
            ""score"": 818015
          },
          {
            ""id"": 25,
            ""nickName"": ""Guest82"",
            ""lastActiveTime"": 1474187920,
            ""countryCode"": ""RU"",
            ""vkontakte"": ""271527343"",
            ""place"": 16,
            ""score"": 773790
          },
          {
            ""id"": 32,
            ""nickName"": ""Guest567"",
            ""lastActiveTime"": 1473947241,
            ""countryCode"": ""RU"",
            ""place"": 17,
            ""score"": 750690
          },
          {
            ""id"": 89,
            ""nickName"": ""Guest922"",
            ""lastActiveTime"": 1473826834,
            ""countryCode"": ""RU"",
            ""place"": 18,
            ""score"": 537020
          },
          {
            ""id"": 45,
            ""nickName"": ""Guest557"",
            ""lastActiveTime"": 1473324331,
            ""countryCode"": ""RU"",
            ""place"": 19,
            ""score"": 533690
          },
          {
            ""id"": 110,
            ""nickName"": ""Demetri"",
            ""lastActiveTime"": 1474028228,
            ""countryCode"": ""RU"",
            ""place"": 20,
            ""score"": 520145
          },
          {
            ""id"": 7,
            ""nickName"": ""Guest55"",
            ""lastActiveTime"": 1467373516,
            ""countryCode"": ""RU"",
            ""place"": 70,
            ""score"": 500
          },
          {
            ""id"": 1,
            ""nickName"": ""Guest672"",
            ""lastActiveTime"": 1469439804,
            ""countryCode"": ""RU"",
            ""place"": 71,
            ""score"": 500
          },
          {
            ""id"": 131,
            ""nickName"": ""Хер Майор"",
            ""lastActiveTime"": 1474029307,
            ""countryCode"": ""RU"",
            ""place"": 72,
            ""score"": 500
          },
          {
            ""id"": 128,
            ""nickName"": ""kirito"",
            ""lastActiveTime"": 1474028957,
            ""countryCode"": ""RU"",
            ""place"": 73,
            ""score"": 500
          },
          {
            ""id"": 139,
            ""nickName"": ""Guest829"",
            ""lastActiveTime"": 1474192012,
            ""countryCode"": ""RU"",
            ""place"": 74,
            ""score"": 500
          },
          {
            ""id"": 145,
            ""nickName"": ""Guest372"",
            ""lastActiveTime"": 1474198109,
            ""countryCode"": ""RU"",
            ""place"": 75,
            ""score"": 500
          }
        ]
      },
      ""clans"": {
        ""leaderBoard"": [
          {
            ""id"": ""2"",
            ""name"": ""ррррр"",
            ""slogan"": null,
            ""image"": ""17"",
            ""membersCount"": ""1"",
            ""place"": 1,
            ""score"": 6705
          },
          {
            ""id"": ""3"",
            ""name"": ""аарп"",
            ""slogan"": null,
            ""image"": ""2"",
            ""membersCount"": ""1"",
            ""place"": 2,
            ""score"": 450
          }
        ]
      }
    },
    ""week"": {
      ""world"": {
        ""leaderBoard"": [
          {
            ""id"": 11,
            ""nickName"": ""mr green"",
            ""lastActiveTime"": 1473771844,
            ""countryCode"": ""RU"",
            ""place"": 1,
            ""score"": 64895
          },
          {
            ""id"": 93,
            ""nickName"": ""qwe123"",
            ""lastActiveTime"": 1473943233,
            ""countryCode"": ""RU"",
            ""place"": 2,
            ""score"": 47440
          },
          {
            ""id"": 94,
            ""nickName"": ""1232454"",
            ""lastActiveTime"": 1474029210,
            ""countryCode"": ""RU"",
            ""place"": 3,
            ""score"": 42520
          },
          {
            ""id"": 89,
            ""nickName"": ""Guest922"",
            ""lastActiveTime"": 1473826834,
            ""countryCode"": ""RU"",
            ""place"": 4,
            ""score"": 38610
          },
          {
            ""id"": 99,
            ""nickName"": ""Nika"",
            ""lastActiveTime"": 1473938030,
            ""countryCode"": ""RU"",
            ""place"": 5,
            ""score"": 25365
          },
          {
            ""id"": 21,
            ""nickName"": ""ddewqfd"",
            ""lastActiveTime"": 1474027090,
            ""countryCode"": ""RU"",
            ""place"": 6,
            ""score"": 24940
          },
          {
            ""id"": 25,
            ""nickName"": ""Guest82"",
            ""lastActiveTime"": 1474187920,
            ""countryCode"": ""RU"",
            ""vkontakte"": ""271527343"",
            ""place"": 7,
            ""score"": 16930
          },
          {
            ""id"": 114,
            ""nickName"": ""Andrey D"",
            ""lastActiveTime"": 1474011990,
            ""countryCode"": ""RU"",
            ""place"": 8,
            ""score"": 14785
          },
          {
            ""id"": 2,
            ""nickName"": ""Splitstrim"",
            ""lastActiveTime"": 1474186828,
            ""countryCode"": ""RU"",
            ""place"": 9,
            ""score"": 14630
          },
          {
            ""id"": 17,
            ""nickName"": ""DenSid"",
            ""lastActiveTime"": 1474194674,
            ""countryCode"": ""RU"",
            ""place"": 10,
            ""score"": 11815
          },
          {
            ""id"": 78,
            ""nickName"": ""Guest539"",
            ""lastActiveTime"": 1473938997,
            ""countryCode"": ""RU"",
            ""place"": 11,
            ""score"": 11680
          },
          {
            ""id"": 106,
            ""nickName"": ""Лютый"",
            ""lastActiveTime"": 1473945685,
            ""countryCode"": ""RU"",
            ""place"": 12,
            ""score"": 7825
          },
          {
            ""id"": 83,
            ""nickName"": ""Guest512"",
            ""lastActiveTime"": 1473921159,
            ""countryCode"": ""RU"",
            ""place"": 13,
            ""score"": 7260
          },
          {
            ""id"": 37,
            ""nickName"": ""Guest832"",
            ""lastActiveTime"": 1474192284,
            ""countryCode"": ""RU"",
            ""vkontakte"": ""271523319"",
            ""place"": 14,
            ""score"": 5490
          },
          {
            ""id"": 8,
            ""nickName"": ""TheSaMonsta"",
            ""lastActiveTime"": 1474186741,
            ""countryCode"": ""RU"",
            ""place"": 15,
            ""score"": 5030
          },
          {
            ""id"": 90,
            ""nickName"": ""Guest771"",
            ""lastActiveTime"": 1473932668,
            ""countryCode"": ""RU"",
            ""place"": 16,
            ""score"": 4830
          },
          {
            ""id"": 133,
            ""nickName"": ""SEGA NAGIBALKA"",
            ""lastActiveTime"": 1474196096,
            ""countryCode"": ""RU"",
            ""place"": 17,
            ""score"": 4625
          },
          {
            ""id"": 16,
            ""nickName"": ""Demetri"",
            ""lastActiveTime"": 1473942182,
            ""countryCode"": ""RU"",
            ""place"": 18,
            ""score"": 3525
          },
          {
            ""id"": 105,
            ""nickName"": ""Guest153"",
            ""lastActiveTime"": 1474026565,
            ""countryCode"": ""RU"",
            ""place"": 19,
            ""score"": 3020
          },
          {
            ""id"": 107,
            ""nickName"": ""LONDON"",
            ""lastActiveTime"": 1473943552,
            ""countryCode"": ""RU"",
            ""place"": 20,
            ""score"": 2795
          },
          {
            ""id"": 119,
            ""nickName"": ""Nika"",
            ""lastActiveTime"": 1474003565,
            ""countryCode"": ""RU"",
            ""place"": 41,
            ""score"": 540
          },
          {
            ""id"": 128,
            ""nickName"": ""kirito"",
            ""lastActiveTime"": 1474028957,
            ""countryCode"": ""RU"",
            ""place"": 42,
            ""score"": 520
          },
          {
            ""id"": 103,
            ""nickName"": ""Грубо говоря"",
            ""lastActiveTime"": 1473946446,
            ""countryCode"": ""RU"",
            ""place"": 43,
            ""score"": 500
          },
          {
            ""id"": 131,
            ""nickName"": ""Хер Майор"",
            ""lastActiveTime"": 1474029307,
            ""countryCode"": ""RU"",
            ""place"": 44,
            ""score"": 500
          },
          {
            ""id"": 139,
            ""nickName"": ""Guest829"",
            ""lastActiveTime"": 1474192012,
            ""countryCode"": ""RU"",
            ""place"": 45,
            ""score"": 500
          },
          {
            ""id"": 145,
            ""nickName"": ""Guest372"",
            ""lastActiveTime"": 1474198109,
            ""countryCode"": ""RU"",
            ""place"": 46,
            ""score"": 500
          },
          {
            ""id"": 132,
            ""nickName"": ""Demetri"",
            ""lastActiveTime"": 1474183588,
            ""countryCode"": ""RU"",
            ""place"": 47,
            ""score"": 260
          },
          {
            ""id"": 146,
            ""nickName"": ""Demetri"",
            ""lastActiveTime"": 1474196148,
            ""countryCode"": ""RU"",
            ""place"": 48,
            ""score"": 135
          }
        ]
      },
      ""country"": {
        ""name"": ""Russian Federation"",
        ""code"": ""RU"",
        ""leaderBoard"": [
          {
            ""id"": 11,
            ""nickName"": ""mr green"",
            ""lastActiveTime"": 1473771844,
            ""countryCode"": ""RU"",
            ""place"": 1,
            ""score"": 64895
          },
          {
            ""id"": 93,
            ""nickName"": ""qwe123"",
            ""lastActiveTime"": 1473943233,
            ""countryCode"": ""RU"",
            ""place"": 2,
            ""score"": 47440
          },
          {
            ""id"": 94,
            ""nickName"": ""1232454"",
            ""lastActiveTime"": 1474029210,
            ""countryCode"": ""RU"",
            ""place"": 3,
            ""score"": 42520
          },
          {
            ""id"": 89,
            ""nickName"": ""Guest922"",
            ""lastActiveTime"": 1473826834,
            ""countryCode"": ""RU"",
            ""place"": 4,
            ""score"": 38610
          },
          {
            ""id"": 99,
            ""nickName"": ""Nika"",
            ""lastActiveTime"": 1473938030,
            ""countryCode"": ""RU"",
            ""place"": 5,
            ""score"": 25365
          },
          {
            ""id"": 21,
            ""nickName"": ""ddewqfd"",
            ""lastActiveTime"": 1474027090,
            ""countryCode"": ""RU"",
            ""place"": 6,
            ""score"": 24940
          },
          {
            ""id"": 25,
            ""nickName"": ""Guest82"",
            ""lastActiveTime"": 1474187920,
            ""countryCode"": ""RU"",
            ""vkontakte"": ""271527343"",
            ""place"": 7,
            ""score"": 16930
          },
          {
            ""id"": 114,
            ""nickName"": ""Andrey D"",
            ""lastActiveTime"": 1474011990,
            ""countryCode"": ""RU"",
            ""place"": 8,
            ""score"": 14785
          },
          {
            ""id"": 2,
            ""nickName"": ""Splitstrim"",
            ""lastActiveTime"": 1474186828,
            ""countryCode"": ""RU"",
            ""place"": 9,
            ""score"": 14630
          },
          {
            ""id"": 17,
            ""nickName"": ""DenSid"",
            ""lastActiveTime"": 1474194674,
            ""countryCode"": ""RU"",
            ""place"": 10,
            ""score"": 11815
          },
          {
            ""id"": 78,
            ""nickName"": ""Guest539"",
            ""lastActiveTime"": 1473938997,
            ""countryCode"": ""RU"",
            ""place"": 11,
            ""score"": 11680
          },
          {
            ""id"": 106,
            ""nickName"": ""Лютый"",
            ""lastActiveTime"": 1473945685,
            ""countryCode"": ""RU"",
            ""place"": 12,
            ""score"": 7825
          },
          {
            ""id"": 83,
            ""nickName"": ""Guest512"",
            ""lastActiveTime"": 1473921159,
            ""countryCode"": ""RU"",
            ""place"": 13,
            ""score"": 7260
          },
          {
            ""id"": 37,
            ""nickName"": ""Guest832"",
            ""lastActiveTime"": 1474192284,
            ""countryCode"": ""RU"",
            ""vkontakte"": ""271523319"",
            ""place"": 14,
            ""score"": 5490
          },
          {
            ""id"": 8,
            ""nickName"": ""TheSaMonsta"",
            ""lastActiveTime"": 1474186741,
            ""countryCode"": ""RU"",
            ""place"": 15,
            ""score"": 5030
          },
          {
            ""id"": 90,
            ""nickName"": ""Guest771"",
            ""lastActiveTime"": 1473932668,
            ""countryCode"": ""RU"",
            ""place"": 16,
            ""score"": 4830
          },
          {
            ""id"": 133,
            ""nickName"": ""SEGA NAGIBALKA"",
            ""lastActiveTime"": 1474196096,
            ""countryCode"": ""RU"",
            ""place"": 17,
            ""score"": 4625
          },
          {
            ""id"": 16,
            ""nickName"": ""Demetri"",
            ""lastActiveTime"": 1473942182,
            ""countryCode"": ""RU"",
            ""place"": 18,
            ""score"": 3525
          },
          {
            ""id"": 105,
            ""nickName"": ""Guest153"",
            ""lastActiveTime"": 1474026565,
            ""countryCode"": ""RU"",
            ""place"": 19,
            ""score"": 3020
          },
          {
            ""id"": 107,
            ""nickName"": ""LONDON"",
            ""lastActiveTime"": 1473943552,
            ""countryCode"": ""RU"",
            ""place"": 20,
            ""score"": 2795
          },
          {
            ""id"": 119,
            ""nickName"": ""Nika"",
            ""lastActiveTime"": 1474003565,
            ""countryCode"": ""RU"",
            ""place"": 41,
            ""score"": 540
          },
          {
            ""id"": 128,
            ""nickName"": ""kirito"",
            ""lastActiveTime"": 1474028957,
            ""countryCode"": ""RU"",
            ""place"": 42,
            ""score"": 520
          },
          {
            ""id"": 103,
            ""nickName"": ""Грубо говоря"",
            ""lastActiveTime"": 1473946446,
            ""countryCode"": ""RU"",
            ""place"": 43,
            ""score"": 500
          },
          {
            ""id"": 131,
            ""nickName"": ""Хер Майор"",
            ""lastActiveTime"": 1474029307,
            ""countryCode"": ""RU"",
            ""place"": 44,
            ""score"": 500
          },
          {
            ""id"": 139,
            ""nickName"": ""Guest829"",
            ""lastActiveTime"": 1474192012,
            ""countryCode"": ""RU"",
            ""place"": 45,
            ""score"": 500
          },
          {
            ""id"": 145,
            ""nickName"": ""Guest372"",
            ""lastActiveTime"": 1474198109,
            ""countryCode"": ""RU"",
            ""place"": 46,
            ""score"": 500
          },
          {
            ""id"": 132,
            ""nickName"": ""Demetri"",
            ""lastActiveTime"": 1474183588,
            ""countryCode"": ""RU"",
            ""place"": 47,
            ""score"": 260
          },
          {
            ""id"": 146,
            ""nickName"": ""Demetri"",
            ""lastActiveTime"": 1474196148,
            ""countryCode"": ""RU"",
            ""place"": 48,
            ""score"": 135
          }
        ]
      },
      ""region"": {
        ""name"": ""Chelyabinsk"",
        ""leaderBoard"": [
          {
            ""id"": 11,
            ""nickName"": ""mr green"",
            ""lastActiveTime"": 1473771844,
            ""countryCode"": ""RU"",
            ""place"": 1,
            ""score"": 64895
          },
          {
            ""id"": 93,
            ""nickName"": ""qwe123"",
            ""lastActiveTime"": 1473943233,
            ""countryCode"": ""RU"",
            ""place"": 2,
            ""score"": 47440
          },
          {
            ""id"": 94,
            ""nickName"": ""1232454"",
            ""lastActiveTime"": 1474029210,
            ""countryCode"": ""RU"",
            ""place"": 3,
            ""score"": 42520
          },
          {
            ""id"": 89,
            ""nickName"": ""Guest922"",
            ""lastActiveTime"": 1473826834,
            ""countryCode"": ""RU"",
            ""place"": 4,
            ""score"": 38610
          },
          {
            ""id"": 99,
            ""nickName"": ""Nika"",
            ""lastActiveTime"": 1473938030,
            ""countryCode"": ""RU"",
            ""place"": 5,
            ""score"": 25365
          },
          {
            ""id"": 21,
            ""nickName"": ""ddewqfd"",
            ""lastActiveTime"": 1474027090,
            ""countryCode"": ""RU"",
            ""place"": 6,
            ""score"": 24940
          },
          {
            ""id"": 25,
            ""nickName"": ""Guest82"",
            ""lastActiveTime"": 1474187920,
            ""countryCode"": ""RU"",
            ""vkontakte"": ""271527343"",
            ""place"": 7,
            ""score"": 16930
          },
          {
            ""id"": 114,
            ""nickName"": ""Andrey D"",
            ""lastActiveTime"": 1474011990,
            ""countryCode"": ""RU"",
            ""place"": 8,
            ""score"": 14785
          },
          {
            ""id"": 2,
            ""nickName"": ""Splitstrim"",
            ""lastActiveTime"": 1474186828,
            ""countryCode"": ""RU"",
            ""place"": 9,
            ""score"": 14630
          },
          {
            ""id"": 17,
            ""nickName"": ""DenSid"",
            ""lastActiveTime"": 1474194674,
            ""countryCode"": ""RU"",
            ""place"": 10,
            ""score"": 11815
          },
          {
            ""id"": 78,
            ""nickName"": ""Guest539"",
            ""lastActiveTime"": 1473938997,
            ""countryCode"": ""RU"",
            ""place"": 11,
            ""score"": 11680
          },
          {
            ""id"": 106,
            ""nickName"": ""Лютый"",
            ""lastActiveTime"": 1473945685,
            ""countryCode"": ""RU"",
            ""place"": 12,
            ""score"": 7825
          },
          {
            ""id"": 83,
            ""nickName"": ""Guest512"",
            ""lastActiveTime"": 1473921159,
            ""countryCode"": ""RU"",
            ""place"": 13,
            ""score"": 7260
          },
          {
            ""id"": 37,
            ""nickName"": ""Guest832"",
            ""lastActiveTime"": 1474192284,
            ""countryCode"": ""RU"",
            ""vkontakte"": ""271523319"",
            ""place"": 14,
            ""score"": 5490
          },
          {
            ""id"": 8,
            ""nickName"": ""TheSaMonsta"",
            ""lastActiveTime"": 1474186741,
            ""countryCode"": ""RU"",
            ""place"": 15,
            ""score"": 5030
          },
          {
            ""id"": 90,
            ""nickName"": ""Guest771"",
            ""lastActiveTime"": 1473932668,
            ""countryCode"": ""RU"",
            ""place"": 16,
            ""score"": 4830
          },
          {
            ""id"": 133,
            ""nickName"": ""SEGA NAGIBALKA"",
            ""lastActiveTime"": 1474196096,
            ""countryCode"": ""RU"",
            ""place"": 17,
            ""score"": 4625
          },
          {
            ""id"": 16,
            ""nickName"": ""Demetri"",
            ""lastActiveTime"": 1473942182,
            ""countryCode"": ""RU"",
            ""place"": 18,
            ""score"": 3525
          },
          {
            ""id"": 105,
            ""nickName"": ""Guest153"",
            ""lastActiveTime"": 1474026565,
            ""countryCode"": ""RU"",
            ""place"": 19,
            ""score"": 3020
          },
          {
            ""id"": 107,
            ""nickName"": ""LONDON"",
            ""lastActiveTime"": 1473943552,
            ""countryCode"": ""RU"",
            ""place"": 20,
            ""score"": 2795
          },
          {
            ""id"": 119,
            ""nickName"": ""Nika"",
            ""lastActiveTime"": 1474003565,
            ""countryCode"": ""RU"",
            ""place"": 41,
            ""score"": 540
          },
          {
            ""id"": 128,
            ""nickName"": ""kirito"",
            ""lastActiveTime"": 1474028957,
            ""countryCode"": ""RU"",
            ""place"": 42,
            ""score"": 520
          },
          {
            ""id"": 103,
            ""nickName"": ""Грубо говоря"",
            ""lastActiveTime"": 1473946446,
            ""countryCode"": ""RU"",
            ""place"": 43,
            ""score"": 500
          },
          {
            ""id"": 131,
            ""nickName"": ""Хер Майор"",
            ""lastActiveTime"": 1474029307,
            ""countryCode"": ""RU"",
            ""place"": 44,
            ""score"": 500
          },
          {
            ""id"": 139,
            ""nickName"": ""Guest829"",
            ""lastActiveTime"": 1474192012,
            ""countryCode"": ""RU"",
            ""place"": 45,
            ""score"": 500
          },
          {
            ""id"": 145,
            ""nickName"": ""Guest372"",
            ""lastActiveTime"": 1474198109,
            ""countryCode"": ""RU"",
            ""place"": 46,
            ""score"": 500
          },
          {
            ""id"": 132,
            ""nickName"": ""Demetri"",
            ""lastActiveTime"": 1474183588,
            ""countryCode"": ""RU"",
            ""place"": 47,
            ""score"": 260
          },
          {
            ""id"": 146,
            ""nickName"": ""Demetri"",
            ""lastActiveTime"": 1474196148,
            ""countryCode"": ""RU"",
            ""place"": 48,
            ""score"": 135
          }
        ]
      },
      ""clans"": {
        ""leaderBoard"": [
          {
            ""id"": ""2"",
            ""name"": ""ррррр"",
            ""slogan"": null,
            ""image"": ""17"",
            ""membersCount"": ""1"",
            ""place"": 1,
            ""score"": 6705
          },
          {
            ""id"": ""3"",
            ""name"": ""аарп"",
            ""slogan"": null,
            ""image"": ""2"",
            ""membersCount"": ""1"",
            ""place"": 2,
            ""score"": 450
          }
        ]
      }
    }
  },
  ""status"": 1
}
";

            /*
             * ,
          {
            ""id"": 132,
            ""nickName"": ""Demetri1"",
            ""lastActiveTime"": 1474183588,
            ""countryCode"": ""RU"",
            ""place"": 47,
            ""score"": 260
          },
          {
            ""id"": 146,
            ""nickName"": ""Demetri2"",
            ""lastActiveTime"": 1474196148,
            ""countryCode"": ""RU"",
            ""place"": 48,
            ""score"": 135
          },
          {
            ""id"": 147,
            ""nickName"": ""Demetri3"",
            ""lastActiveTime"": 1474196148,
            ""countryCode"": ""RU"",
            ""place"": 49,
            ""score"": 135
          },
          {
            ""id"": 148,
            ""nickName"": ""Demetri4"",
            ""lastActiveTime"": 1474196148,
            ""countryCode"": ""RU"",
            ""place"": 50,
            ""score"": 135
          },
          {
            ""id"": 149,
            ""nickName"": ""Demetri5"",
            ""lastActiveTime"": 1474196148,
            ""countryCode"": ""RU"",
            ""place"": 51,
            ""score"": 135
          }
             */
            var friendsStatsMockup = @"
{
  ""stats"": [
    {
      ""id"": ""25"",
      ""nickName"": ""Guest82"",
      ""vkontakte"": ""271527343"",
      ""score"": ""773875"",
      ""lastActiveTime"": 1475068824
    },
    {
      ""id"": ""167"",
      ""nickName"": ""дима3д"",
      ""score"": ""4615"",
      ""lastActiveTime"": 1475154217
    },
    {
      ""id"": ""26"",
      ""nickName"": ""Guest228"",
      ""score"": ""4190"",
      ""lastActiveTime"": 1474885501
    },
    {
      ""id"": ""97"",
      ""nickName"": ""454654654654"",
      ""score"": ""1470"",
      ""lastActiveTime"": 1475222121
    },
    {
      ""id"": ""158"",
      ""nickName"": ""Guest99"",
      ""score"": ""500"",
      ""lastActiveTime"": 1475226280
    },
    {
      ""id"": ""159"",
      ""nickName"": ""Guest105"",
      ""score"": ""500"",
      ""lastActiveTime"": 1475226010
    },
    {
      ""id"": ""109"",
      ""nickName"": ""Guest186"",
      ""score"": ""500"",
      ""lastActiveTime"": 1475049472
    }
  ],
  ""status"": 1,
  ""profileChanges"": {
    ""Fuel"": 7.5104166666667
  }
}";
#endregion

            var scoresData = MiniJSON.Json.Deserialize(statsMockup) as JSONObject;
            scoresData = scoresData["stats"] as JSONObject;

            var friendsScoresDataDict = MiniJSON.Json.Deserialize(friendsStatsMockup) as JSONObject;
            var friendsScoresData = friendsScoresDataDict["stats"] as List<object>;

            var deltas = new List<LeaderboardDelta>
                {
                    //new LeaderboardDelta(null, "friends",
                    //    new LeaderboardItem(100, -500), new LeaderboardItem(2, 100001)),

                    new LeaderboardDelta("allTime", "world",
                        new LeaderboardItem(50, -500), new LeaderboardItem(1, 100001)),
                    new LeaderboardDelta("week", "region",
                        new LeaderboardItem(21, -500), new LeaderboardItem(21, 100001)),
                    //new LeaderboardDelta("allTime", "world",
                    //    new LeaderboardItem(22, -500), new LeaderboardItem(21, 100001)),
                };

            foreach (var deltaToCheckBounds in GetDeltasToShow(deltas, scoresData, friendsScoresData))
            {
                if (!CheckScoresDataForDelta(deltaToCheckBounds, scoresData, friendsScoresData))
                    continue;

                deltaToShow = deltaToCheckBounds;
                break;
            }

            var voiceEventId = 
                (int)(ProfileInfo.IsPlayerVip 
                    ? VoiceEventKey.LeaderBoardWindowVIP 
                    : VoiceEventKey.LeaderBoardWindow);

            GUIPager.EnqueuePage("ScoresAnimation", true, true, voiceEventId);
        }
    }

    [Serializable]
    public class LeaderboardDelta
    {
        public string time;
        public string area;

        public LeaderboardItem oldLeaderboardItem;
        public LeaderboardItem newLeaderboardItem;

        public Animation? animation;

        public int PlaceDelta { get { return oldLeaderboardItem.place - newLeaderboardItem.place; } }
        public int ScoreDelta { get { return newLeaderboardItem.score - oldLeaderboardItem.score; } }

        public LeaderboardDelta(string time, string area, LeaderboardItem oldLeaderboardItem, LeaderboardItem newLeaderboardItem,
            Animation? animation = null)
        {
            this.time = time;
            this.area = area;
            this.oldLeaderboardItem = oldLeaderboardItem;
            this.newLeaderboardItem = newLeaderboardItem;
            this.animation = animation;
        }

        // In case of severe OCD break glass
        //public static bool operator ==(LeaderboardDelta a, LeaderboardDelta b)
        //{
        //    // If both are null, or both are same instance, return true.
        //    if (ReferenceEquals(a, b))
        //        return true;

        //    // Return true if the fields match.
        //    if (((object)a != null) && ((object)b != null))
        //        return a.time == b.time
        //            && a.area == b.area
        //            && a.oldLeaderboardItem == b.oldLeaderboardItem
        //            && a.newLeaderboardItem == b.newLeaderboardItem
        //            && a.animation == b.animation;

        //    // If one is null, but not both, return true if fields have default values.
        //    if ((object)a == null)
        //    {
        //        if (string.IsNullOrEmpty(b.time)
        //            && string.IsNullOrEmpty(b.area)
        //            && b.oldLeaderboardItem == null
        //            && b.newLeaderboardItem == null
        //            && b.animation == null)
        //            return true;
        //    }

        //    if ((object)b == null)
        //    {
        //        if (string.IsNullOrEmpty(a.time)
        //            && string.IsNullOrEmpty(a.area)
        //            && a.oldLeaderboardItem == null
        //            && a.newLeaderboardItem == null
        //            && a.animation == null)
        //            return true;
        //    }

        //    return false;
        //}

        //public static bool operator !=(LeaderboardDelta a, LeaderboardDelta b)
        //{
        //    return !(a == b);
        //}

        public override string ToString()
        {
            return string.Format("[{0}: time = {1}, area = {2}, oldLeaderboardItem = {3}, newLeaderboardItem = {4}, " +
                "PlaceDelta = {5}, ScoreDelta = {6}, animation = {7}]", base.ToString(), time, area, oldLeaderboardItem, newLeaderboardItem, PlaceDelta, ScoreDelta, animation);
        }
    }

    [Serializable]
    public class LeaderboardItem
    {
        public int place;
        public int score;

        public LeaderboardItem(int place, int score)
        {
            this.place = place;
            this.score = score;
        }

        // In case of severe OCD break glass
        //public static bool operator ==(LeaderboardItem a, LeaderboardItem b)
        //{
        //    // If both are null, or both are same instance, return true.
        //    if (ReferenceEquals(a, b))
        //        return true;

        //    // Return true if the fields match.
        //    if (((object)a != null) && ((object)b != null))
        //        return a.place == b.place && a.score == b.score;

        //    // If one is null, but not both, return true if fields have default values.
        //    if ((object)a == null)
        //    {
        //        if (b.place == default(int) && b.score == default(int))
        //            return true;
        //    }

        //    if ((object)b == null)
        //    {
        //        if (a.place == default(int) && a.score == default(int))
        //            return true;
        //    }

        //    return false;
        //}

        //public static bool operator !=(LeaderboardItem a, LeaderboardItem b)
        //{
        //    return !(a == b);
        //}

        public override string ToString()
        {
            return string.Format("[{0}: place = {1}, score = {2}]", base.ToString(), place, score);
        }
    }
}
