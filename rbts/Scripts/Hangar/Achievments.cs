using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SocialPlatforms;
#if UNITY_ANDROID
using GooglePlayGames;
#endif

public class Achievments : MonoBehaviour
{
    public static Achievments Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        Messenger.Subscribe(EventId.FullCalcBattleStatistics, SendAchievements);
        Messenger.Subscribe(EventId.ModuleReceived, ModuleReceived);
        //Messenger.Subscribe(EventId.AfterHangarInit, DebugStatistics);
    }

    void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.FullCalcBattleStatistics, SendAchievements);
        Messenger.Unsubscribe(EventId.ModuleReceived, ModuleReceived);
        //Messenger.Unsubscribe(EventId.AfterHangarInit, DebugStatistics);
        Instance = null;
    }

    private void Start()
    {
        if(AchievementsIds.CurrentPlayService == AchievementsIds.PlayServiceType.Disabled)//Отправляем из Start() чтобы другие скрипты успели подписаться на это событие
        {
            Messenger.Send(EventId.GameCenterDisabled, new EventInfo_SimpleEvent());
#if !UNITY_EDITOR //for test
            Destroy(this);
#endif
            return;
        }
    }

    /*private void DebugStatistics(EventId id, EventInfo info)
    {
        DebugStatistics();
        //CheckForDeadAchievementIds();
        //CheckForCompletedAchievements();
    }*/

    private void SendAchievements(EventId id, EventInfo info)
    {
#if UNITY_EDITOR //for test
        CheckForCompletedAchievements();
#endif

#if !UNITY_EDITOR
            AchievementsIds.Init();
            CheckForDeadAchievementIds();//Проверяем парсятся ли сохраненные в профиле ачивки (раньше сохранялись сторовские коды по ошибке, вместо самого енама)
		    CheckForCompletedAchievements(); // Проверяем ачивки в любом случае, если не сможем подрубиться к плейсервису – сохраняем их в профиле
#if !(UNITY_WEBPLAYER || UNITY_WEBGL)
		        AuthenticateIfNeeded();
#else
                ReportAllAchievements();
#endif
#endif
    }

    /// <summary>
    /// Происходит только для iOS, Mac, Android
    /// </summary>
    public void AuthenticateIfNeeded(Action<bool> callback = null)
    {
        if (GameData.IsGame(Game.AmazonBuild)) return;
        //Debug.Log ("Achievments. AfterHangarInit 5");
        if (Social.localUser == null)
        {
            if(callback != null)
                callback(false);
            return;
        }
        if(Social.localUser.authenticated)
        {
            OnSuccessfullAuthentication();
            if (callback != null)
                callback(true);
        }
        else
        {
#if UNITY_ANDROID
            PlayGamesPlatform.DebugLogEnabled = true;
            PlayGamesPlatform.Activate();
#endif

            Social.localUser.Authenticate(success =>
            {
                if (callback != null)
                    callback(success);//Колбэк должен сам следить в ангаре мы или нет, чтобы не запутаться...
                if (!GameData.IsHangarScene)
                {
                    Debug.LogError("Ignore social authenticating, bacause we are not in hangar.");
                    return;
                }
                if (success)
                    OnSuccessfullAuthentication();
                else
                    Debug.LogError("Achievments.AuthenticateIfNeeded(). Auth FAILED !!!");
            });
        }
    }

    /// <summary>
    /// Происходит только для iOS, Mac, Android
    /// </summary>
    private void OnSuccessfullAuthentication()
    {
        Social.ReportScore(
            ProfileInfo.Experience,
            AchievementsIds.GetLeaderboardId(),
            null);

        // Request loaded achievements, and register a callback for processing them.
        Social.LoadAchievements(ProcessLoadedAchievements);
    }

    /// <summary>
    /// Происходит только для iOS, Mac, Android
    /// </summary>
    private void ProcessLoadedAchievements(IAchievement[] achievements)
    {
        string myAchievements = String.Empty;

        foreach (IAchievement achievement in achievements)
        {
            myAchievements += "\t" +
                achievement.id + " " +
                achievement.percentCompleted + " " +
                achievement.completed + " " +
                achievement.lastReportedDate + "\n";

            //Если не смогли определить енам ачивки по ее сторовскому коду - выходим
            AchievementsIds.Id achId = AchievementsIds.Id.Pilot;
            if (!AchievementsIds.GetAchievementIdFromStoreId(achievement.id, ref achId))
                continue;
            string achIdString = achId.ToString();

            //Если ачивка в профиле есть и показывается как отправленная, а стор говорит что она не зарепорчена - исправляем профиль
            if (ProfileInfo.savedAchievements.ContainsKey(achIdString) && Convert.ToBoolean(ProfileInfo.savedAchievements[achIdString]) && !achievement.completed)
                ProfileInfo.savedAchievements[achIdString] = false;

            //Если в сторе ачивка выполнена, а в профиле - нет - сбрасываем ачивку в сторе, тока это не работает. Сделано на всякий случай.
            //if (achievement.completed && !ProfileInfo.savedAchievements.ContainsKey(achievement.id))
            //    ResetAchievement(achievement.id);

            //Выставляем ачивке признак того что она зарепорчена в сторе, чтобы не отправлять повторно
            if (achievement.completed && ProfileInfo.savedAchievements.ContainsKey(achIdString))
                ProfileInfo.savedAchievements[achIdString] = true;
        }

        ReportAllAchievements();
    }

    private void ReportAllAchievements()
    {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        if (Debug.isDebugBuild)
        {
            string achievementsReport = String.Empty;

            foreach (var ach in ProfileInfo.savedAchievements)
                achievementsReport += string.Format("Achieve {0} Shown: {1}\n", ach.Key, ach.Value);

            DT.Log("Achievements: " + achievementsReport);
        }

        List<string> buffer = new List<string>(ProfileInfo.savedAchievements.Keys);

	    foreach (var key in buffer)
	    {
            if (!Convert.ToBoolean(ProfileInfo.savedAchievements[key]))
	        {
                ProfileInfo.savedAchievements[key] = true;
		        ReportAchievementWeb(key);
	            return;
	        }
		}
#else

        foreach (var ach in ProfileInfo.savedAchievements)
            if (!Convert.ToBoolean(ach.Value))
                ReportAchievement(ach.Key);
#endif
    }

    private void ShowMyLeaderboard(tk2dUIItem item)
    {
        if (Social.localUser == null)
        {
            return;
        }

        if (!Social.localUser.authenticated)
        {
            AuthenticateIfNeeded((bool success) =>
            {
                if (success)
                    Social.ShowLeaderboardUI();
            });
        }
        else
            Social.ShowLeaderboardUI();
    }

    private void ShowMyAchievments(tk2dUIItem item)
    {
        if(Social.localUser == null)
        {
            return;
        }

        if (!Social.localUser.authenticated)
        {
            AuthenticateIfNeeded((bool success) =>
            {
                if (success)
                    Social.ShowAchievementsUI();
            });
        }
        else
            Social.ShowAchievementsUI();

    }

    private void ReportAchievement(string achievmentID)
    {
        AchievementsIds.Id aId = AchievementsIds.Id.Pilot;
        if (!AchievementsIds.GetAchieveIdFromEnumString(achievmentID, ref aId))
            return;
        string storeCode = AchievementsIds.GetAchievementIdFromDataDic(aId);
        Social.ReportProgress(
            storeCode,
            100.0,
            success =>
            {
                if (GameData.IsHangarScene)
                    DT.Log("ReportAchievement {0}. Result: {1}", aId,success);
            });
    }

    private void ReportAchievementWeb(string achievmentID)
    {
        DT.Log("Start to reportAchievement {0}", achievmentID);

        try
        {
            if (!Enum.IsDefined(typeof(AchievementsIds.Id), achievmentID))
                return;
            AchievementsIds.Id id = (AchievementsIds.Id)Enum.Parse(typeof(AchievementsIds.Id), achievmentID);
            if (id >= AchievementsIds.Id.EnemyOfRobots)
            {
                string text = Localizer.GetText("text" + id + "Post", Application.productName);
                SocialSettings.GetSocialService().PostAchievement(id, text);
            }
        }
        catch (ArgumentException) { /* CL.LogError("Enum.Parse ArgumentException"); */ }
    }

    private void AddAchievment(AchievementsIds.Id achievmentId)
    {
        if (!ProfileInfo.savedAchievements.ContainsKey(achievmentId.ToString()))
            ProfileInfo.savedAchievements.Add(achievmentId.ToString(), false);
    }

#region Check For Completed Achievements
    private void CheckForCompletedAchievements()
    {
#if !(UNITY_WEBPLAYER || UNITY_WEBGL)
        DeleteUnearnedAchievements();
#endif

        if (BattleStatisticsManager.BattleStats["ProperEndBattle"] == 1)
            CheckSurvivorAchievment();
        
        CheckKillsInARowAchievment(BattleStatisticsManager.OverallBattleStats["MaxKillsInARow"]);
        CheckBattleCountAchievment(BattleStatisticsManager.OverallBattleStats["BattlesCount"]);
        if (BattleStatisticsManager.OverallBattleStats["TotalPlayedTime"] >= 360000) AddAchievment(AchievementsIds.Id.BigFan); //3600

        CheckWinBattlesCountAchievment(BattleStatisticsManager.OverallBattleStats["WinBattlesCount"]);
        //CheckVehicleUpgradesAchievment(BattleStatisticsManager.OverallBattleStats["VehicleUpgrades"]);
        CheckTotalKillAchievment(BattleStatisticsManager.OverallBattleStats["TotalKillRobots"], BattleStatisticsManager.OverallBattleStats["TotalKillTanks"]); //TotalFrags TotalDeaths
        if (BattleStatisticsManager.OverallBattleStats["TotalDamageMyRobot"] >= 100000) AddAchievment(AchievementsIds.Id.Terminator);
        if (BattleStatisticsManager.OverallBattleStats["TotalDamageMyTank"] >= 100000) AddAchievment(AchievementsIds.Id.FullMetalJacket);
        if (BattleStatisticsManager.OverallBattleStats["TotalBonusTaken"] >= 50) AddAchievment(AchievementsIds.Id.Gatherer);
        if (BattleStatisticsManager.OverallBattleStats["TotalRevengeDone"] >= 50) AddAchievment(AchievementsIds.Id.Nemesis);
        CheckTotalKillWithOneShotAchievment(BattleStatisticsManager.OverallBattleStats["TotalKillWithOneShot"]);

        if (new BattleStatisticsManager.Maps(10, BattleStatisticsManager.OverallBattleStats["VisitedMaps"]).Count >= 3) AddAchievment(AchievementsIds.Id.Pathfinder);
        if (BattleStatisticsManager.BattleStats["FuelLastOne"] == 1) //if (ProfileInfo.Fuel < 1)
        {
            AddAchievment(AchievementsIds.Id.ToTheLast);
            //Log("!!!!!!Fuel(UntilLast): " + ProfileInfo.Fuel);
        }
        if (VehiclePool.Instance.Items.Length == ProfileInfo.vehicleUpgrades.Count) AddAchievment(AchievementsIds.Id.Collector);
        if (ProfileInfo.Clan != null && ProfileInfo.Clan.Rank == Tanks.Models.Clan.ClanRank.commander) AddAchievment(AchievementsIds.Id.Commander);
        CheckReachedLevelAchievement(); // В последнюю очередь.
        
        ProfileInfo.SaveToServer();
        //DebugStatistics();
    }

    /*void DebugStatistics()
    {
        Log("TotalDamageMyRobot(Terminator_100kDamage): " + BattleStatisticsManager.OverallBattleStats["TotalDamageMyRobot"]);
        Log("TotalDamageMyTank(StrongArmor_100kDamage): " + BattleStatisticsManager.OverallBattleStats["TotalDamageMyTank"]);
        Log("TotalBonusTaken(BonusCollector): " + BattleStatisticsManager.OverallBattleStats["TotalBonusTaken"]);
        Log("TotalRevengeDone(Nemesis): " + BattleStatisticsManager.OverallBattleStats["TotalRevengeDone"]);
        Log("VisitedMaps Value " + BattleStatisticsManager.OverallBattleStats["VisitedMaps"]);
        Log("Fuel(UntilLast): " + ProfileInfo.Fuel);
        Log("VehiclePool.Instance.Items.Length == ProfileInfo.vehicleUpgrades.Count " + VehiclePool.Instance.Items.Length + " " + ProfileInfo.vehicleUpgrades.Count);
        Log("TotalKillWithOneShot(Sniper100, Saboteur50): " + BattleStatisticsManager.OverallBattleStats["TotalKillWithOneShot"]);
        Log("killRobots(10-50, Neutral10_10, DoubleAgent50_50, TotalDestruction500-1000): " + BattleStatisticsManager.OverallBattleStats["TotalKillRobots"] + " killTanks: " + BattleStatisticsManager.OverallBattleStats["TotalKillTanks"]);
        Log("KillsInARow: " + BattleStatisticsManager.OverallBattleStats["MaxKillsInARow"]);
        Log("BattleCount: " + BattleStatisticsManager.OverallBattleStats["BattlesCount"]);
        Log("WinBattlesCount(MasterCombat, Winner): " + BattleStatisticsManager.OverallBattleStats["WinBattlesCount"]);

        Log("Upgrades(Engineer, Technician): " + BattleStatisticsManager.OverallBattleStats["VehicleUpgrades"]);
        Log("Survivor EndBattle(1): " + BattleStatisticsManager.BattleStats["ProperEndBattle"] + " isRoomFull: " + BattleStatisticsManager.isNotSingleRoom + " Deaths(0): " + BattleStatisticsManager.BattleStats["Deaths"]);
        Log("Level: " + ProfileInfo.Level);
    }*/

    private void CheckForDeadAchievementIds()
    {
        List<string> achievesToDeleteFromProfile = new List<string>();
        foreach (var achieve in ProfileInfo.savedAchievements)
            if (!Enum.IsDefined(typeof(AchievementsIds.Id), achieve.Key))
                achievesToDeleteFromProfile.Add(achieve.Key);

        for (int i = 0; i < achievesToDeleteFromProfile.Count; i++)
            if (ProfileInfo.savedAchievements.ContainsKey(achievesToDeleteFromProfile[i]))
                ProfileInfo.savedAchievements.Remove(achievesToDeleteFromProfile[i]);
    }

    void ModuleReceived(EventId id, EventInfo ei)
    {
        BattleStatisticsManager.OverallBattleStats["VehicleUpgrades"]++;

        //fix not save hangar achievement
        CheckVehicleUpgradesAchievment(BattleStatisticsManager.OverallBattleStats["VehicleUpgrades"]);
        ProfileInfo.SaveToServer();
    }

    /// <summary>
    /// Удаляем незаработанные ачивки - фикс для тех, кто еще не успел обновиться - чтоб они не получили незаработанные очивки
    /// (Последствие того что в профиле все ачивки показаны как полученные)
    /// </summary>
    private void DeleteUnearnedAchievements()
    {
        List<string> achievesToDeleteFromProfile = new List<string>();
        foreach(var achieve in ProfileInfo.savedAchievements)
        {
            AchievementsIds.Id aId = AchievementsIds.Id.Pilot;
            if (!AchievementsIds.GetAchieveIdFromEnumString(achieve.Key, ref aId)) continue;
            int achieveLevel = (int)aId;
            //Чиним ачивки уровней
            if ( achieveLevel <= ((int)AchievementsIds.Id.Capitan))
            {
                if (ProfileInfo.Level < achieveLevel)
                    achievesToDeleteFromProfile.Add(achieve.Key);
            }
            //Чиним ачивки DaysInARow - НЕЛЬЗЯ УДАЛЯТЬ, ИБО ЕСЛИ Я ПРОПУЩУ ДЕНЬ, ЗАЙДУ В ИГРУ - АЧИВКА СБОРОСИТСЯ, А Я МОЖЕТ ДО ЭТОГО 50 ДНЕЙ ПОДРЯД ИГРАЛ
            //if(aId == AchievementsIds.Id.Fans && ProfileInfo.daysInARow < 50)
            //    achievesToDeleteFromProfile.Add(achieve.Key);
            //if (aId == AchievementsIds.Id.Admirer && ProfileInfo.daysInARow < 30)
            //    achievesToDeleteFromProfile.Add(achieve.Key);
            //if (aId == AchievementsIds.Id.Gambler && ProfileInfo.daysInARow < 10)
            //    achievesToDeleteFromProfile.Add(achieve.Key);

            if(BattleStatisticsManager.OverallBattleStats != null)
            {
                //CheckTimeAchievment
                if (BattleStatisticsManager.OverallBattleStats.ContainsKey("TotalPlayedTime"))
                {
                    if (aId == AchievementsIds.Id.BigFan && BattleStatisticsManager.OverallBattleStats["TotalPlayedTime"] < 100)
                        achievesToDeleteFromProfile.Add(achieve.Key);
                }

                //CheckBattleCountAchievment
                if (BattleStatisticsManager.OverallBattleStats.ContainsKey("BattlesCount"))
                {
                    if (aId == AchievementsIds.Id.Fighter && BattleStatisticsManager.OverallBattleStats["BattlesCount"] < 100)
                        achievesToDeleteFromProfile.Add(achieve.Key);
                }

                // MaxKillsInARow
                if (BattleStatisticsManager.OverallBattleStats.ContainsKey("MaxKillsInARow"))
                {
                    if (aId == AchievementsIds.Id.Berserk && BattleStatisticsManager.OverallBattleStats["MaxKillsInARow"] < 5)
                        achievesToDeleteFromProfile.Add(achieve.Key);
                    if (aId == AchievementsIds.Id.Disintegrator && BattleStatisticsManager.OverallBattleStats["MaxKillsInARow"] < 10)
                        achievesToDeleteFromProfile.Add(achieve.Key);
                }
            }
        }

        for(int i = 0; i < achievesToDeleteFromProfile.Count; i++)
            if (ProfileInfo.savedAchievements.ContainsKey(achievesToDeleteFromProfile[i]))
            {
                //Debug.Log("DELETE UNEARNED ACHIEVE FROM PROFILE: " + achievesToDeleteFromProfile[i]);
                ProfileInfo.savedAchievements.Remove(achievesToDeleteFromProfile[i]);
            }
                
    }

    private void CheckReachedLevelAchievement()
    {
        if (ProfileInfo.Level > 50)
        {
            return;
        }
        
        if (ProfileInfo.Level >= 50) AddAchievment(AchievementsIds.Id.Capitan);
        if (ProfileInfo.Level >= 40) AddAchievment(AchievementsIds.Id.LieutenantCommander);
        if (ProfileInfo.Level >= 30) AddAchievment(AchievementsIds.Id.WarentOfficer);
        if (ProfileInfo.Level >= 20) AddAchievment(AchievementsIds.Id.Foreman);
        if (ProfileInfo.Level >= 10) AddAchievment(AchievementsIds.Id.Pilot);

        //for (int i = 2; i <= ProfileInfo.Level; i++) AddAchievment((AchievementsIds.Id)(i));
    }

    private void CheckBattleCountAchievment(int battleCount)
    {
        if (battleCount >= 300)     AddAchievment(AchievementsIds.Id.Veteran);
        if (battleCount >= 200)     AddAchievment(AchievementsIds.Id.Warrior);
        if (battleCount >= 100)     AddAchievment(AchievementsIds.Id.Fighter);
    }

    private void CheckKillsInARowAchievment(int kills)
    {
        if (kills >= 10) AddAchievment(AchievementsIds.Id.Disintegrator);
        if (kills >= 5)  AddAchievment(AchievementsIds.Id.Berserk);
        if (kills == 0) AddAchievment(AchievementsIds.Id.Peacemaker);
    }

    private void CheckSurvivorAchievment()
    {
        if (BattleStatisticsManager.isNotSingleRoom)
            if (BattleStatisticsManager.BattleStats["Deaths"] == 0)
                AddAchievment(AchievementsIds.Id.SwiftOne);
    }

    private void CheckTotalKillAchievment(int killRobots, int killTanks)
    {
        if (killRobots >= 50)
        {
            AddAchievment(AchievementsIds.Id.SmasherOfTanks);
            if (killTanks >= 50) AddAchievment(AchievementsIds.Id.DoubleAgent);
        }

        if (killRobots >= 10)
        {
            AddAchievment(AchievementsIds.Id.EnemyOfRobots);
            if (killTanks >= 10) AddAchievment(AchievementsIds.Id.NeutralSide);
        }

        if (killTanks >= 50) AddAchievment(AchievementsIds.Id.SmasherOfRobots);
        if (killTanks >= 10) AddAchievment(AchievementsIds.Id.EnemyOfTanks);

        int kill = killRobots + killTanks;
        if (kill >= 1000) AddAchievment(AchievementsIds.Id.TotalAnnihilation);
        if (kill >= 500) AddAchievment(AchievementsIds.Id.Technophobe);
    }

    private void CheckWinBattlesCountAchievment(int count)
    {
        if (count >= 100) AddAchievment(AchievementsIds.Id.MasterCombat);
        if (count >= 50) AddAchievment(AchievementsIds.Id.Winner);
    }

    private void CheckVehicleUpgradesAchievment(int upgrades)
    {
        if (upgrades >= 100) AddAchievment(AchievementsIds.Id.Engineer);
        if (upgrades >= 50) AddAchievment(AchievementsIds.Id.Artificer);
    }

    private void CheckTotalKillWithOneShotAchievment(int kills)
    {
        if (kills >= 100) AddAchievment(AchievementsIds.Id.Avenger);
        if (kills >= 50) AddAchievment(AchievementsIds.Id.Saboteur);
    }

    #endregion
}
