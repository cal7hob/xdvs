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

        #if !UNITY_EDITOR
            DT.Log ("Cur play service is {0}", AchievementsIds.CurrentPlayService);
            Dispatcher.Subscribe(EventId.AfterHangarInit, AfterHangarInit);
        #endif
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.AfterHangarInit, AfterHangarInit);
        Instance = null;
    }

    private void Start()
    {
        if(AchievementsIds.CurrentPlayService == AchievementsIds.PlayServiceType.Disabled)//Отправляем из Start() чтобы другие скрипты успели подписаться на это событие
        {
            Dispatcher.Send(EventId.GameCenterDisabled, new EventInfo_SimpleEvent());
            Destroy(this);
            return;
        }
    }

    private void AfterHangarInit(EventId id, EventInfo info)
    {
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
            Debug.LogError("!!! Social.localUser == NULL !!! Achievements will be disabled!");
            if(callback != null)
                callback(false);
            return;
        }
        if(Social.localUser.authenticated)
        {
            Debug.Log("Social.localUser.authenticated! Loading achievements...");
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
        Debug.Log("Achievments.OnSuccessfullAuthentication()");
        Social.ReportScore(
            ProfileInfo.Experience,
            AchievementsIds.GetLeaderboardId(),
            success => DT.Log(
                "Reporting Scores to leaderboard {0}. Result: {1}",
                AchievementsIds.GetLeaderboardId(),
                success));

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
            AchievementsIds.Id achId = AchievementsIds.Id.ReachedLevel2;
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

        Debug.Log("Got " + achievements.Length + " achievements from " + AchievementsIds.CurrentPlayService.ToString() + "\n" + myAchievements);

        ReportAllAchievements();
    }

    private void ReportAllAchievements()
    {
        Debug.LogFormat("ReportAllAchievments(): savedAchievements.Count = {0}", ProfileInfo.savedAchievements.Count);

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
            Debug.LogError("Cant ShowMyLeaderboard! Social.localUser == null!");
            return;
        }
        DT.Log("Show LEADERBOARD id = {0}, authorized = {1}!", AchievementsIds.GetLeaderboardId(), Social.localUser.authenticated);

        if (!Social.localUser.authenticated)
        {
            AuthenticateIfNeeded((bool success) =>
            {
                if (success)
                    Social.ShowLeaderboardUI();
                else
                    Debug.LogError("Cant ShowLeaderboardUI! Auth failed!");
            });
        }
        else
            Social.ShowLeaderboardUI();
    }

    private void ShowMyAchievments(tk2dUIItem item)
    {
        if(Social.localUser == null)
        {
            Debug.LogError("Cant ShowMyAchievments! Social.localUser == null!");
            return;
        }

        if (!Social.localUser.authenticated)
        {
            AuthenticateIfNeeded((bool success) =>
            {
                if (success)
                    Social.ShowAchievementsUI();
                else
                    Debug.LogError("Cant ShowMyAchievments! Auth failed!");
            });
        }
        else
            Social.ShowAchievementsUI();

    }

    private void ReportAchievement(string achievmentID)
    {
        AchievementsIds.Id aId = AchievementsIds.Id.ReachedLevel2;
        if (!AchievementsIds.GetAchieveIdFromEnumString(achievmentID, ref aId))
            return;
        string storeCode = AchievementsIds.GetAchievementIdFromDataDic(aId);
        Debug.LogFormat("Start to report achievement {0}, storeCode = {1}", aId, storeCode);
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
            if (!Enum.IsDefined(typeof (AchievementsIds.Id), achievmentID)) return;
            AchievementsIds.Id id = (AchievementsIds.Id)Enum.Parse(typeof(AchievementsIds.Id), achievmentID);
            if (id >= AchievementsIds.Id.NewbieTankMan)
            {
                string suffix;
                if(GameData.IsGame(Game.SpaceJet))
                    suffix = "SJ";
                else if (GameData.IsGame(Game.BattleOfWarplanes))
                    suffix = "BW";
                else if (GameData.IsGame(Game.WingsOfWar))
                    suffix = "BW";
                else if (GameData.IsGame(Game.BattleOfHelicopters))
                    suffix = "BW";
                else 
                    suffix = "";
                string text = Localizer.GetText("text" + suffix + id + "Post",
                    Application.productName);
                SocialSettings.GetSocialService().PostAchievement(id, text);
            }
        }
        catch (ArgumentException) { /* Debug.LogError("Enum.Parse ArgumentException"); */ }
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
        CheckMileageAchievment(BattleStatisticsManager.OverallBattleStats["TotalMileage"]);
        CheckBattleCountAchievment(BattleStatisticsManager.OverallBattleStats["BattlesCount"]);
        CheckTimeAchievment(BattleStatisticsManager.OverallBattleStats["TotalPlayedTime"]);
        CheckDaysInARowAchievment(ProfileInfo.daysInARow);
        CheckReachedLevelAchievement(); // В последнюю очередь.
    }

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

    /// <summary>
    /// Удаляем незаработанные ачивки - фикс для тех, кто еще не успел обновиться - чтоб они не получили незаработанные очивки
    /// (Последствие того что в профиле все ачивки показаны как полученные)
    /// </summary>
    private void DeleteUnearnedAchievements()
    {
        List<string> achievesToDeleteFromProfile = new List<string>();
        foreach(var achieve in ProfileInfo.savedAchievements)
        {
            AchievementsIds.Id aId = AchievementsIds.Id.ReachedLevel2;
            if (!AchievementsIds.GetAchieveIdFromEnumString(achieve.Key, ref aId))
                continue;
            int achieveLevel = (int)aId;
            //Чиним ачивки уровней
            if ( achieveLevel <= ((int)AchievementsIds.Id.ReachedLevel36))
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
                    if (aId == AchievementsIds.Id.Colonel && BattleStatisticsManager.OverallBattleStats["TotalPlayedTime"] < 360000)
                        achievesToDeleteFromProfile.Add(achieve.Key);
                    if (aId == AchievementsIds.Id.Sergeant && BattleStatisticsManager.OverallBattleStats["TotalPlayedTime"] < 36000)
                        achievesToDeleteFromProfile.Add(achieve.Key);
                    if (aId == AchievementsIds.Id.Ordinary && BattleStatisticsManager.OverallBattleStats["TotalPlayedTime"] < 3600)
                        achievesToDeleteFromProfile.Add(achieve.Key);
                }

                //CheckBattleCountAchievment
                if (BattleStatisticsManager.OverallBattleStats.ContainsKey("BattlesCount"))
                {
                    if (aId == AchievementsIds.Id.ProfessionalTanker && BattleStatisticsManager.OverallBattleStats["BattlesCount"] < 1000)
                        achievesToDeleteFromProfile.Add(achieve.Key);
                    if (aId == AchievementsIds.Id.ExperiencedTanker && BattleStatisticsManager.OverallBattleStats["BattlesCount"] < 100)
                        achievesToDeleteFromProfile.Add(achieve.Key);
                    if (aId == AchievementsIds.Id.NewbieTankMan && BattleStatisticsManager.OverallBattleStats["BattlesCount"] < 10)
                        achievesToDeleteFromProfile.Add(achieve.Key);
                }

                //CheckMileageAchievment
                if (BattleStatisticsManager.OverallBattleStats.ContainsKey("TotalMileage"))
                {
                    if (aId == AchievementsIds.Id.Wanderer && BattleStatisticsManager.OverallBattleStats["TotalMileage"] < 1000000)
                        achievesToDeleteFromProfile.Add(achieve.Key);
                    if (aId == AchievementsIds.Id.Travelers && BattleStatisticsManager.OverallBattleStats["TotalMileage"] < 100000)
                        achievesToDeleteFromProfile.Add(achieve.Key);
                    if (aId == AchievementsIds.Id.Scout && BattleStatisticsManager.OverallBattleStats["TotalMileage"] < 10000)
                        achievesToDeleteFromProfile.Add(achieve.Key);
                }

                // MaxKillsInARow
                if (BattleStatisticsManager.OverallBattleStats.ContainsKey("MaxKillsInARow"))
                {
                    if (aId == AchievementsIds.Id.Terminator && BattleStatisticsManager.OverallBattleStats["MaxKillsInARow"] < 4)
                        achievesToDeleteFromProfile.Add(achieve.Key);
                    if (aId == AchievementsIds.Id.Killer && BattleStatisticsManager.OverallBattleStats["MaxKillsInARow"] < 3)
                        achievesToDeleteFromProfile.Add(achieve.Key);
                    if (aId == AchievementsIds.Id.Shredder && BattleStatisticsManager.OverallBattleStats["MaxKillsInARow"] < 2)
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
        if (ProfileInfo.Level > 36)
            return;

        for (int i = 2; i <= ProfileInfo.Level; i++)
            AddAchievment((AchievementsIds.Id)(i));
    }

    private void CheckBattleCountAchievment(int battleCount)
    {
        if (battleCount >= 1000)    AddAchievment(AchievementsIds.Id.ProfessionalTanker);
        if (battleCount >= 100)     AddAchievment(AchievementsIds.Id.ExperiencedTanker);
        if (battleCount >= 10)      AddAchievment(AchievementsIds.Id.NewbieTankMan);
    }

    private void CheckMileageAchievment(int kilometers)
    {
        if (kilometers >= 1000000)  AddAchievment(AchievementsIds.Id.Wanderer);
        if (kilometers >= 100000)   AddAchievment(AchievementsIds.Id.Travelers);
        if (kilometers >= 10000)    AddAchievment(AchievementsIds.Id.Scout);
    }

    private void CheckDaysInARowAchievment(int daysInARow)
    {
        if (daysInARow >= 50)    AddAchievment(AchievementsIds.Id.Fans);
        if (daysInARow >= 30)    AddAchievment(AchievementsIds.Id.Admirer);
        if (daysInARow >= 10)    AddAchievment(AchievementsIds.Id.Gambler);
    }

    private void CheckKillsInARowAchievment(int kills)
    {
        if (kills >= 4)  AddAchievment(AchievementsIds.Id.Terminator);
        if (kills >= 3)  AddAchievment(AchievementsIds.Id.Killer);
        if (kills >= 2)  AddAchievment(AchievementsIds.Id.Shredder);
    }

    private void CheckSurvivorAchievment()
    {
        if (BattleStatisticsManager.isRoomFull)
            if (BattleStatisticsManager.BattleStats["Deaths"] == 0)
                AddAchievment(AchievementsIds.Id.Survivor);
    }

    private void CheckTimeAchievment(int time)
    {
        if (time >= 360000) AddAchievment(AchievementsIds.Id.Colonel);
        if (time >= 36000)  AddAchievment(AchievementsIds.Id.Sergeant);
        if (time >= 3600)   AddAchievment(AchievementsIds.Id.Ordinary);
    }

#endregion

#region Debug OnGui
    //private void OnGUI()
    //{
    //    if (Button("Reset Achievements"))
    //    {
    //        savedAchievements = new Dictionary<string, object>();
    //        ProfileInfo.SaveToServer();
    //    }

    //    foreach (Id id in Enum.GetValues(typeof(Id)))
    //    {
    //        if (id < Id.NewbieTankMan)
    //            continue;

    //        if (!Button(id.ToString()))
    //            continue;

    //        AddAchievment(id);

    //        ProfileInfo.SaveToServer();
    //    }
    //}

    //private void OnGUI()
    //{
        //if (GUI.Button(new Rect(20, 20, 200, 75), "View Leaderboard"))
        //    Social.ShowLeaderboardUI();
        //if (GUI.Button(new Rect(20, 100, 200, 75), "View Achievements"))
        //    Social.ShowAchievementsUI();

        // RESET ALL ACHIEVEMENTS.
        //if (GUI.Button(new Rect(20, 260, 200, 75), "Reset Achievements"))
        //{
        //    UnityEngine.SocialPlatforms.GameCenter.GameCenterPlatform.ResetAllAchievements(resetResult =>
        //    {
        //        Debug.Log(resetResult ? "Achievements have been Reset" : "Achievement reset failure.");
        //        ProfileInfo.savedAchievements = new Dictionary<string, object>();
        //        ProfileInfo.SaveToServer();
        //    });
        //}

        //if (GUI.Button(new Rect(225, 20, 200, 75), "Report Achievement 8"))
        //    ReportAchievement(AchievementsIds.GetAchievementId(AchievementsIds.Id.ReachedLevel8));

        //if (GUI.Button(new Rect(225, 100, 200, 75), "Reset Achievement 8"))
        //    ResetAchievement(AchievementsIds.GetAchievementId(AchievementsIds.Id.ReachedLevel8));

        //if (GUI.Button(new Rect(225, 180, 200, 75), "Report Achievement 10"))
        //    ReportAchievement(GetAchievementId(Id.ReachedLevel10));

        //if (GUI.Button(new Rect(225, 260, 200, 75), "Report Achievement 11"))
        //    ReportAchievement(GetAchievementId(Id.ReachedLevel11));

        //if (GUI.Button(new Rect(225, 340, 200, 75), "Report Achievement 12"))
        //    ReportAchievement(GetAchievementId(Id.ReachedLevel12));
    //}

#endregion
}
