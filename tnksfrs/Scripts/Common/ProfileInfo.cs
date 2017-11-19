#if !(UNITY_STANDALONE_OSX || UNITY_WEBPLAYER || UNITY_WEBGL)
#define TOUCH_SCREEN
#endif

using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Linq;
#if !(UNITY_WEBPLAYER || UNITY_WEBGL) && !UNITY_WSA
using System.Net.Configuration;
#endif
#if !UNITY_WSA
using System.Runtime.Serialization.Formatters.Binary; //
#endif
using Http;
using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using CodeStage.AntiCheat.ObscuredTypes;
using Tanks.Models;
using Random = UnityEngine.Random;
using Request = Http.Request;
using UnityEngine.SceneManagement;
using XD;

public static class ProfileInfo
{
    private const int               START_SILVER = 5000;
    private const int               START_GOLD = 0;
    public const int                CHAT_BAN_1HOUR = 3600;
    private const float             MAX_TIME_INTERVAL = 3600 * 24 * 14;

    private static ObscuredInt      level = 1;
    private static ObscuredInt      experience = 0;
    private static int              nextExperience = 320;
    private static int              prevExperience = 240;
    private static ObscuredInt      silver = 0;
    private static ObscuredInt      gold = 0;
    private static string           countryCode;
    private static bool             importantUpdate;
    private static string           version = "";
    private static string           marketURL = "";
    private static ObscuredInt      playerPrivilege = 0;

    private static ObscuredString   playerName = "Guest";
    private static ObscuredString   playerEmail  = "";
    private static bool             playerEmailConfirmed = false;
    public static ObscuredInt       playerId = 0;
    private static ObscuredBool     isPlayerVip = false;
    private static ObscuredBool     isCheater = false;
    private static ObscuredInt      vipExpirationDate;
    private static ObscuredFloat    vipExpRate = 1.5f;
    private static ObscuredFloat    vipSilverRate = 1.2f;
    private static ObscuredInt      tutorialIndex;
    private static Clan             clan;
    private static ObscuredInt      hangarSlots;

    public static ObscuredInt       currentVehicle = 0;
    public static ObscuredInt       dailyBonusIndex;
    public static ObscuredInt       dailyBonusDaysCount;
    public static ObscuredBool      goldKitAwardIsObtained = true;//По умолчанию считаем что награда дана, в новый день сервер пришлет false и покажется окно награды, после того как пользователь возьмет награду - сервер пришлет true
    public static ObscuredInt       goldKitAwardVal;
    public static ObscuredInt       launchesCount;
    public static ObscuredDouble    nextDayServerTime;
    public static ObscuredBool      dailyBonusIsObtained;
    public static ObscuredDouble    lastVisit;
    public static ObscuredLong      lastProfileSaveTimestamp = 0;
    public static ObscuredString    appGUID = "";
    public static ObscuredBool      ratedForBonus;
    public static ObscuredBool      nickEntered;
    public static ObscuredInt       daysInARow;

    /// <summary>
    /// Входит ли страна игрока в число стран со специальными правилами матчмейкинга
    /// </summary>
    public static bool fromSpecialCountry;
    public static Dictionary<int, VehicleUpgrades> vehicleUpgrades = new Dictionary<int, VehicleUpgrades>(10);
    public static List<int> doubleExpVehicles = new List<int>();
    public static List<SocialAction>            socialActivity = new List<SocialAction>();
    public static bool                          enterForFirstTime = false;
    public static bool                          nickRejected = false;
    public static Dictionary<string, object>    weeklyAwardsDict = new Dictionary<string, object>();
    public static Dictionary<string, object>    clanInfoDict = new Dictionary<string, object>();
    public static ObscuredBool                  useDebugPanelForLogging = false;
    public static ObscuredBool                  isServerLogsEnabled = false;
    public static List<int>                     accomplishedTutorials = null;
    public static Vector3                       initialAcceleration = Vector3.zero;

    public static Dictionary<string, XDevsOffersStatus> xdevsOffers = new Dictionary<string, XDevsOffersStatus>();

    // Social settings
    public static ObscuredBool isSocialActivated = false;
    public static ObscuredString fbUserId = "";

    // Battle settings
    public static int lastMapId = -1;

    // LevelUp Award
    public static int lastLevelUpAward = 1;

    /// <summary>
    /// Наличие ачивки в словаре говорит о том что она заработана, true - то, что она показана игроку (если играешь в соц. Сети), или зарепорчена в стор (если на iOS, Mac, Android)
    /// </summary>
    public static Dictionary<string, object> savedAchievements = new Dictionary<string, object>();

    // Player Settings
    public static bool isInvert = false;            // реверс заднего хода или инверт вертикальной оси
    public static bool isFireOnDoubleTap = false;
    public static bool isHideMyFlag = false;
    public static bool isPushForDailyBonus = false;
    public static bool isPushForUpgrade = false;
    public static bool isVoiceDisabled;
    public static int avatarOption;
    public static int controlOption;
    public static int languageIndex = -1;

    public static bool isSliderControl
#if TOUCH_SCREEN && !UNITY_EDITOR
    = true;
#else
    = false;
#endif

    // Rate This Game
    public static bool isGameRated = false;

    // Непрочтённые сообщения для FeedbackButton
    public static int unreadMessages;

    /**********************************************************************
     * Контроль только одного активного сохранения профиля на сервер за раз
     */
    private static bool m_isSaveToServerActive = false;
    private static bool m_isSaveToServerRequired = false;
    private static List<Action<Http.Response, bool>> m_awaitedSaveCallbacks = new List<Action<Http.Response, bool>>();

    [Serializable]
    public enum PlayerPrivileges
    {
        None = 0,
        Moderator,
        Administrator
    }

    [Serializable]
    public enum PriceCurrency
    {
        Silver  = 0,
        Gold    = 1,
    }

    public static PlayerPrivileges PlayerPrivilege
    {
        get
        {
            return (PlayerPrivileges)Convert.ToInt32(playerPrivilege);
        }
    }

    public static AvatarOption AvatarOption
    {
        get
        {
            return (AvatarOption)avatarOption;
        }
    }

    public static ControlOption ControlOption
    {
        get
        {
            return (ControlOption)controlOption;
        }
    }

    public static bool IsVoiceDisablingAvailable
    {
        get
        {
            return StaticContainer.Options.Language == (int)SystemLanguage.Russian;
        }
    }

    public static bool IsBattleTutorial
    {
        get
        {
            return tutorialIndex == (int)Tutorials.BattleTutorial;
        }
    }

    public static bool IsBattleTutorialCompleted
    {
        get
        {
            int battleTutorialIndex = (int)Tutorials.BattleTutorial;

            if (accomplishedTutorials == null || !accomplishedTutorials.Contains(battleTutorialIndex))
            {
                return false;
            }

            return tutorialIndex > (int)Tutorials.BattleTutorial;
        }
    }

    public static ObscuredInt TutorialIndex
    {
        get
        {
            return tutorialIndex;
        }
        set
        {
            Dispatcher.Send(EventId.TutorialIndexChanged, new EventInfo_I(tutorialIndex));
            tutorialIndex = value;
        }
    }
    
    [Serializable]
    public class Price
    {
        public ObscuredInt value;
        public PriceCurrency currency;

        private static Dictionary<Interface, Dictionary<PriceCurrency, Color>> moneyColors = new Dictionary<Interface, Dictionary<PriceCurrency, Color>>
        {
            {Interface.Armada2, new Dictionary<PriceCurrency, Color>
                {
                    {PriceCurrency.Gold, new Color32(210, 135, 55, 255)},
                    {PriceCurrency.Silver, new Color32(130, 175, 175, 255)},//82afaf
                }
            }
        };

        public XD.CurrencyValue ToCurrencyValue()
        {
            return new XD.CurrencyValue(currency == PriceCurrency.Silver ? XD.CurrencyType.Silver : XD.CurrencyType.Gold, value);
        }

        public Price(int _value, PriceCurrency _currency)
        {
            value = _value;
            currency = _currency;
        }

        public Price(Price other)
        {
            value = other.value;
            currency = other.currency;
        }

        /// <summary>
        /// currency.ToString().ToLower();
        /// </summary>
        public string SpriteName
        {
            get
            {
                return currency.ToString().ToLower();
            }
        }

        /// <summary>
        /// value.ToString("N0", GameData.instance.cultureInfo.NumberFormat);
        /// </summary>
        public string ValueFormat_N0
        {
            get
            {
                return value.ToString("N0", GameData.instance.cultureInfo.NumberFormat);
            }
        }

        public static implicit operator bool(Price price)
        {
            return price != null && !HelpTools.Approximately(price.value, 0);
        }

        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>();

            dict["currency"] = currency;
            dict["value"] = value;

            return dict;
        }

        public static Price FromDictionary(Dictionary<string, object> dict)
        {
            JsonPrefs data = new JsonPrefs(dict);
            return new Price(data.ValueInt("value"), (PriceCurrency)Enum.Parse(typeof(PriceCurrency), data.ValueString("currency"), true));
        }

        public override string ToString()
        {
            return string.Format("(currency: {0}, value = {1})", currency, value);
        }


        public string LocalizedValue
        {
            get
            {
                return value.ToString("N0", GameData.instance.cultureInfo.NumberFormat);
            }
        }

        public Color MoneySpecificColor
        {
            get
            {
                if (!moneyColors.ContainsKey(GameData.CurInterface) || !moneyColors[GameData.CurInterface].ContainsKey(currency))
                {
                    return Color.white;
                }

                return moneyColors[GameData.CurInterface][currency];
            }
        }
    }

    public static ObscuredString PlayerName
    {
        get
        {
            return playerName;
        }

        set
        {
            playerName = value;
            Dispatcher.Send(EventId.NickNameChanged, new EventInfo_SimpleEvent());
        }
    }

    public static ObscuredString PlayerEmail
    {
        get
        {
            return playerEmail;
        }
    }

    public static bool PlayerEmailConfirmed
    {
        get
        {
            return playerEmailConfirmed;
        }
    }

    public static string CountryCode
    {
        get
        {
            return countryCode;
        }

        set
        {
            if (String.IsNullOrEmpty(countryCode))
            {
                countryCode = value;
            }
        }
    }

    public static bool WasInBattle
    {
        get
        {
            if (!PlayerPrefs.HasKey("WasInBattle"))
            {
                return WasInBattle = BattleStatisticsManager.OverallBattleStats["BattlesCount"] > 0;
            }

            return Convert.ToBoolean(PlayerPrefs.GetInt("WasInBattle"));
        }
        set
        {
            PlayerPrefs.SetInt("WasInBattle", Convert.ToInt32(value));
        }
    }
    
    public static int Level
    {
        get
        {
            return level;
        }
    }

    public static string MarketURL
    {
        get
        {
            return marketURL;
        }
        set
        {
            marketURL = value;
        }
    }

    public static int ExperiencePoints
    {
        get;
        set;
    }

    public static int Experience
    {
        get
        {
            return experience;
        }
        set
        {
            experience = value;
            int oldLevel = Level;
            CalcLevel();
            //Пока такой проверки достаточно, но вообще надо что то получше, чтобы отсеить первую отсылку после инициализации переменных
            if (Level != oldLevel)
            {
                Dispatcher.Send(EventId.PlayerLevelChanged, new EventInfo_SimpleEvent());
            }
        }
    }

    public static int CurrentUnit
    {
        get
        {
            return currentVehicle;
        }
        set
        {
            currentVehicle = value;
        }
    }

    public static int PrevExperience
    {
        get
        {
            return prevExperience;
        }
    }

    public static int NextExperience
    {
        get
        {
            return nextExperience;
        }
    }

    public static int Silver
    {
        get
        {
            return silver;
        }
    }

    public static int Gold
    {
        get
        {
            return gold;
        }
    }
    
    public static bool IsPlayerVip
    {
        get
        {
            return isPlayerVip;
        }
        set
        {
            if (value == isPlayerVip)
                return;
            isPlayerVip = value;
            Dispatcher.Send(EventId.VipStatusUpdated, new EventInfo_B(isPlayerVip));
        }
    }

    public static bool IsCheater
    {
        get
        {
            return isCheater;
        }
        set
        {
            isCheater = value;
        }
    }

    public static bool IsNewbie
    {
        get
        {
            return BattleStatisticsManager.OverallBattleStats["BattlesCount"] < XD.Constants.NEWBIE_BATTLES_AMOUNT &&
                   currentVehicle == XD.Constants.NEWBIE_VEHICLE_ID;
        }
    }

    public static bool LastSessionVipStatus
    {
        get; set;
    }

    public static int VipExpirationDate
    {
        get
        {
            return vipExpirationDate;
        }
        set
        {
            vipExpirationDate = value;
            IsPlayerVip = (int)GameData.CorrectedCurrentTimeStamp > Clock.YEAR_SECONDS
                          && vipExpirationDate > (int)GameData.CorrectedCurrentTimeStamp;
        }
    }


    public static float VipExpRate
    {
        get
        {
            return vipExpRate;
        }
    }


    public static float VipSilverRate
    {
        get
        {
            return vipSilverRate;
        }
    }

    public static Clan Clan
    {
        get
        {
            return clan;
        }
        set
        {
            if (value == null && clan == null)
                return;

            if (clan != null && clan.Equals(value))
                return;

            clan = value;
            Dispatcher.Send(EventId.ClanChanged, null);
        }
    }

    /* ProfileInfo class itself */
    public static bool CanBuy(Price price)
    {
        return
            //(price.value > 0) &&
            ((price.currency == PriceCurrency.Gold && gold >= price.value) ||
              (price.currency == PriceCurrency.Silver && silver >= price.value));
    }

    public static void ReplenishBalance(Price sum)
    {
        switch (sum.currency)
        {
            case PriceCurrency.Silver:
                silver += sum.value;
                break;
            case PriceCurrency.Gold:
                gold += sum.value;
                break;
        }

        Dispatcher.Send(EventId.ProfileMoneyChange, new EventInfo_II(gold, silver));
    }

    //public static void AddVehicle(int vehicleId, VehicleUpgrades vehicleUpgrades)
    //{
    //    vehicle.Add(vehicleId, vehicleUpgrades);
    //    doubleExpVehicle.Add(vehicleId);
    //    HangarController.Instance.SetActiveDoubleExpText(vehicleId);
    //    HangarController.Instance.VehicleSelectors[vehicleId].sprDoubleExp.SetActive(true);
    //}

    public static void SetPlayerPrivilege(int privilegeInt)
    {
        playerPrivilege = privilegeInt;
    }

    /// <summary>
    /// Сохранение профиля на сервере.
    /// </summary>
    /// <param name="finishCallback">
    /// Коллбэк с параметрами объекта ответа и результата выполнения операции.
    /// </param>
    public static void SaveToServer(Action<Response, bool> finishCallback = null)
    {
        if (m_isSaveToServerActive)
        {
            //Debug.Log ("Save is active! Shedule request...");
            m_isSaveToServerRequired = true;

            if (finishCallback != null)
                m_awaitedSaveCallbacks.Add(finishCallback);

            return;
        }

        m_isSaveToServerActive = true;

        string dataToSave = PrepareForSave();

        Request request = Manager.Instance().CreateRequest("/player/save");
        request.Form.AddField("data", dataToSave);

        long timestampForSave = (long)GameData.CurrentTimeStamp;

        request.Form.AddField("timestamp", (timestampForSave).ToString());
        request.Form.AddField("lastTimestamp", lastProfileSaveTimestamp.ToString());

        //Debug.Log ("lastTimestamp=" + lastProfileSaveTimestamp.ToString () + ", tsForSave=" + tsForSave);
        //Debug.Log("SaveToServer!");
        Manager.StartAsyncRequest(
            request,
            delegate (Response result)
            {
                //Debug.Log("Save profile data result: " + result.text);
                if (result.Data.ContainsKey("profile"))
                {
                    Debug.Log("Profile data load required");

                    if (!LoadProfile(result.Data["profile"]))
                    {
                        Debug.LogError("Can't load profile! result: " + result.text);

                        if (finishCallback != null)
                            finishCallback(result, false);

                        SaveFinished(result, false);

                        Manager.ReportStats(
                            location: "profile",
                            action: "savingFailed",
                            query: new Dictionary<string, string>
                            {
                                { "clientData", dataToSave },
                                { "response", result.text }
                            });

                        return;
                    }

                    Debug.Log("Profile data load success");
                }
                else
                {
                    lastProfileSaveTimestamp = timestampForSave;
                }

                if (finishCallback != null)
                    finishCallback(result, true);

                SaveFinished(result, true);
            },
            delegate (Response result)
            {
                Debug.LogWarning("Save profile data fail: " + result.error);

                Manager.ReportStats(
                    location: "profile",
                    action: "savingFailed",
                    query: new Dictionary<string, string>
                    {
                        { "clientData", dataToSave },
                        { "response", result.text },
                        { "error", result.error }
                    });

                if (finishCallback != null)
                    finishCallback(result, false);

                SaveFinished(result, false);
            });
    }

    public static bool LoadProfile(object profileData)
    {
        Debug.Log("Load Profile!");
        var receivedProfileData = profileData as Dictionary<string, object>;
        if (receivedProfileData == null)
        {
            Debug.LogError("Can't get profile data for loading!");
            return false;
        }

        if (!receivedProfileData.ContainsKey("data")
            || !receivedProfileData.ContainsKey("timestamp")
            || !receivedProfileData.ContainsKey("playerId"))
        {
            Debug.LogError("Can't get 'data', 'timestamp' or 'playerId' from profile data for loading!");
            return false;
        }

        playerId = Convert.ToInt32(receivedProfileData["playerId"]); // maybe move it to ApplyLoadedData?
        var data = receivedProfileData["data"] as Dictionary<string, object>;
        long timestamp = Convert.ToInt64(receivedProfileData["timestamp"]);

        return ApplyLoadedData(data, timestamp);
    }

    private static void SaveFinished(Response response, bool isSuccess)
    {
        if (m_isSaveToServerRequired)
        {
            //Debug.Log ("Run sheduled save");
            m_isSaveToServerActive = false;
            m_isSaveToServerRequired = false;
            SaveToServer();
            return;
        }
        //if (m_awaitedSaveCallbacks.Count > 0) {
        //    Debug.Log ("Run sheduled callbacks");
        //}
        while (m_awaitedSaveCallbacks.Count > 0)
        {
            m_awaitedSaveCallbacks[0](response, isSuccess);
            m_awaitedSaveCallbacks.RemoveAt(0);
        }
        m_isSaveToServerActive = false;
        //Debug.Log ("All saves done!");
    }

    public static IEnumerator Load()
    {
        yield return null;

        // Загружаем старые настройки громкости если есть
        try
        {
            if (ObscuredPrefs.HasKey("MusicVolume") && !PlayerPrefs.HasKey("MusicVolume"))
            {
                PlayerPrefs.SetFloat("MusicVolume", ObscuredPrefs.GetFloat("MusicVolume"));
                PlayerPrefs.SetFloat("SoundVolume", ObscuredPrefs.GetFloat("SoundVolume"));
                PlayerPrefs.Save();
            }
        }
        catch
        {
        }

        appGUID = PlayerPrefs.GetString("GUID");

        // Отключаем в вебверсии загрузку профиля
        LoadDefaults();
    }

    public static string AppGUID
    {
        get
        {
            if (string.IsNullOrEmpty(appGUID))
            {
                appGUID = PlayerPrefs.GetString(StaticType.ServerList.Instance<IServerList>().ServerName + "_GUID", "");
            }

            return appGUID;
        }

        set
        {
            appGUID = value;
            PlayerPrefs.SetString(StaticType.ServerList.Instance<IServerList>().ServerName + "_GUID", appGUID);
        }
    }

    public static string PrepareForSave()
    {
        var dict = new Dictionary<string, object>();

        dict["Gold"] = (int)gold;
        dict["Silver"] = (int)silver;
        dict["DoubleExpTanks"] = doubleExpVehicles;
        dict["CompletedTutorials"] = accomplishedTutorials;
        dict["InitialAcceleration"] = new Dictionary<string, float>()
        {
            {"x", Settings.InitialAcceleration.x},
            {"y", Settings.InitialAcceleration.y},
            {"z", Settings.InitialAcceleration.z}
        };

        //BattleStatisticsManager.CalcOverallBattleStatistics();
        dict["OverallBattleStatisticsJson"] = MiniJSON.Json.Serialize(BattleStatisticsManager.OverallBattleStats);
        dict["LaunchesCount"] = (int)launchesCount;
        dict["TutorialIndex"] = (int)tutorialIndex;
        dict["Achievements"] = savedAchievements;

        // Параметры из окна настроек игры.
        dict["Invert"] = isInvert;
        dict["SliderControl"] = isSliderControl;
        dict["FireOnDoubleTap"] = isFireOnDoubleTap;
        dict["HideMyFlag"] = isHideMyFlag;
        dict["PushForDailyBonus"] = isPushForDailyBonus;
        dict["PushForUpgrade"] = isPushForUpgrade;
        dict["VoiceDisabled"] = isVoiceDisabled;
        dict["AvatarOption"] = avatarOption;
        dict["ControlOption"] = controlOption;
        dict["LanguageIndex"] = languageIndex;
        dict["GameMode"] = (int)GameData.Mode;
        //DT.LogError("PrepareFoSave GameMode = {0}", dict["GameMode"]);

        dict["LastMapId"] = lastMapId;

        dict["LastSessionVipStatus"] = IsPlayerVip;

        return MiniJSON.Json.Serialize(dict);
    }

    public static bool ApplyLoadedData(Dictionary<string, object> data, long timestamp)
    {
        var prefs = new JsonPrefs(data);

        gold = prefs.ValueInt("Gold", -1);
        if (gold < 0)
        {
            Debug.LogError("Can't read Gold value");
            return false;
        }

        silver = prefs.ValueInt("Silver", -1);
        if (silver < 0)
        {
            Debug.LogError("Can't read Silver value");
            return false;
        }

        PlayerName = prefs.ValueString("NickName", "");
        if (string.IsNullOrEmpty(PlayerName))
        {
            Debug.LogError("Can't read NickName value");
            return false;
        }

        playerEmail = prefs.ValueString("Email", "");

        playerEmailConfirmed = prefs.ValueBool("EmailConfirmed", false);

        if (data.ContainsKey("ExperiencePoints"))
        {
            ExperiencePoints = prefs.ValueInt("ExperiencePoints", 0);
        }

        Experience = prefs.ValueInt("Experience", -1);
        if (Experience < 0)
        {
            Debug.LogError("Can't read Experience value");
            return false;
        }

        lastVisit = prefs.ValueDouble("LastVisit", -1);
        if (lastVisit < 0)
        {
            Debug.LogError("Can't read LastVisit value");
            return false;
        }

        CurrentUnit = prefs.ValueInt("CurrentTank", -1);
        if (CurrentUnit < 0)
        {
            //XD.StaticContainer.UI.Reaction(PSYEvent.TankSelected, 0);
            Debug.LogError("Can't read CurrentTank value");
            return false;
        }

        /*XD.StaticContainer.UI.Reaction(PSYEvent.TankSelected, CurrentVehicle);    
        XD.ColoredDebug.Log("[Send Reaction on " + PSYEvent.TankSelected + "]", null, "green", "orange");*/

        isCheater = prefs.ValueBool("IsCheater");

        // get vip expiration date and time
        VipExpirationDate = prefs.ValueInt("VipExpiration", (int)MiscTools.unixOrigin);
        // update current vip status
        IsPlayerVip = VipExpirationDate > MiscTools.ConvertToUnixTimestamp(GameData.ServerTime.AddSeconds(Clock.YEAR_SECONDS));
        // load last sesion vip status (to update quests)
        LastSessionVipStatus = prefs.ValueBool("LastSessionVipStatus");
        // load extra silver and exp vip multipliers
        vipSilverRate = prefs.ValueFloat("VipSilverRate", vipSilverRate);
        vipExpRate = prefs.ValueFloat("VipExpRate", vipExpRate);

        var l_userTanksDict = prefs.ValueObjectDict("Tanks");
        if (l_userTanksDict.Count() <= 0)
        {
            Debug.LogError("Can't read Tanks value");
            return false;
        }
        GetDoubleExpVehiclesList(prefs.ValueObjectList("DoubleExpTanks"));

        if (data.ContainsKey("CompletedTutorials"))
        {
            GetCompletedTutorialsFromList(data.ExtractOrDefault<List<object>>("CompletedTutorials"));
        }
        else
        {
            accomplishedTutorials = new List<int>() { -1 };
        }

        if (prefs.Contains("Kits"))
        {
            ParseKits(prefs.ValueObjectDict("Kits"));
        }

        if (prefs.Contains("InitialAcceleration"))
        {
            var dict = prefs.ValueObjectDict("InitialAcceleration");
            initialAcceleration = Vector3.right * (float)Convert.ToDouble(dict["x"]) +
                                  Vector3.up * (float)Convert.ToDouble(dict["y"]) +
                                  Vector3.forward * (float)Convert.ToDouble(dict["z"]);
        }

        GetSocialActivity(prefs.ValueObjectList("SocialActivity"));

        lastLevelUpAward = prefs.ValueInt("LastLevelUpAward", 1);
        savedAchievements = prefs.ValueObjectDict("Achievements");
        QuestsInfo.FromDictionary(prefs.ValueObjectList("Quests"));
        SetGameMode(prefs);

        StaticType.BattleStatistics.Instance<IBattleStatistics>().SetOverallStats(prefs.ValueString("OverallBattleStatisticsJson", ""));

        isInvert = prefs.ValueBool("Invert", false);
#if UNITY_WEBPLAYER || UNITY_WEBGL
        isSliderControl = false;
#else
        isSliderControl = prefs.ValueBool("SliderControl", true);
#endif
        isFireOnDoubleTap = prefs.ValueBool("FireOnDoubleTap", false);
        isHideMyFlag = prefs.ValueBool("HideMyFlag", false);
        isPushForDailyBonus = prefs.ValueBool("PushForDailyBonus", true);
        isPushForUpgrade = prefs.ValueBool("PushForUpgrade", true);
        isVoiceDisabled = prefs.ValueBool("VoiceDisabled", false);
        avatarOption = prefs.ValueInt("AvatarOption");
        controlOption = prefs.ValueInt("ControlOption");

#if LANG_CHINESE_ONLY
        languageIndex = (int)Localizer.LocalizationLanguage.Chinese;
#else
        languageIndex = prefs.ValueInt("LanguageIndex", (int)StaticType.Localization.Instance<ILocalization>().CurrentLanguage);
#endif
        launchesCount = prefs.ValueInt("LaunchesCount", 1);

        nextDayServerTime = prefs.ValueDouble("NextDayServerTime");

        dailyBonusIsObtained = prefs.ValueBool("DailyBonusIsObtained");
        dailyBonusIndex = prefs.ValueInt("DailyBonusIndex", 0);

        goldKitAwardIsObtained = prefs.ValueBool("GoldKitAwardIsObtained", true);
        goldKitAwardVal = prefs.ValueInt("GoldKitAwardVal");
        tutorialIndex = !prefs.Contains("TutorialIndex") ? (int)Tutorials.BattleTutorial : prefs.ValueInt("TutorialIndex");

        isSocialActivated = prefs.ValueBool("IsSocialActivated", false);
        lastMapId = prefs.ValueInt("LastMapId");
        nickEntered = prefs.ValueBool("NickEntered", true);
        nickRejected = prefs.ValueBool("NickRejected", true);
        daysInARow = prefs.ValueInt("DaysInARow", 0);
        // Rate This Game
        ratedForBonus = prefs.ValueBool("RatedForBonus", false);
        isGameRated = prefs.ValueBool("IsGameRated", false);
        unreadMessages = prefs.ValueInt("UnreadMessages");

        xdevsOffers = new Dictionary<string, XDevsOffersStatus>();
        if (prefs.Contains("XDevsOffers"))
        {
            foreach (var offerState in prefs.ValueObjectDict("XDevsOffers"))
            {
                try
                {
                    xdevsOffers[offerState.Key] = (XDevsOffersStatus)Enum.Parse(typeof(XDevsOffersStatus), prefs.ValueString("XDevsOffers/" + offerState.Key + "/status"), true);
                }
                catch
                {
                    Debug.LogError("Can't parse offer state for game " + offerState.Key);
                }
            }
        }

        // Weekly Awards
        #region Тестовые данные

        //var testWeeklyAwards = "{\"weeklyAwards\":{\"world\":[{\"place\":1,\"price\":{\"currency\":\"gold\",\"value\":\"300\"}},{\"place\":1,\"price\":{\"currency\":\"silver\",\"value\":\"300000\"}}],\"country\":[{\"place\":2,\"price\":{\"currency\":\"gold\",\"value\":\"100\"},\"name\":\"Russian Federation\"}],\"region\":[{\"place\":1,\"price\":{\"currency\":\"silver\",\"value\":\"50000\"},\"name\":\"Chelyabinsk\"}],\"clans\":[{\"place\":5,\"price\":{\"currency\":\"gold\",\"value\":\"500000\"}}]}}";

        //// Сломанные тестовые данные с неправильной структурой
        //// var testWeeklyAwards = "{\"weeklyAwards\":{\"world\":[{\"place\":1,\"amount\":\"300\",\"currency\":\"gold\"}],\"country\":[{\"name\":\"Russian Federation\",\"place\":1,\"amount\":\"100\",\"currency\":\"gold\"}],\"region\":[{\"name\":\"Chelyabinsk\",\"place\":1,\"amount\":\"50000\",\"currency\":\"silver\"}]}}";

        //weeklyAwardsDict = new JsonPrefs(testWeeklyAwards).ValueObjectDict("weeklyAwards");

        #endregion

        if (prefs.Contains("weeklyAwards"))
        {
            weeklyAwardsDict = prefs.ValueObjectDict("weeklyAwards");
        }

        // Player's clan
        if (prefs.Contains("ClanInfo"))
        {
            clanInfoDict = prefs.ValueObjectDict("ClanInfo");
        }

        GetVehiclesFromDict(l_userTanksDict);

        lastProfileSaveTimestamp = timestamp;
        DT.Log("Profile data loaded, lastProfileSaveTimestamp=" + lastProfileSaveTimestamp.ToString());

        if (prefs.Contains("useDebugPanelForLogging"))
        {
            useDebugPanelForLogging = prefs.ValueBool("useDebugPanelForLogging");
            DT.Log("useDebugPanelForLogging set to " + useDebugPanelForLogging.ToString());
        }

        if (prefs.Contains("isServerLogsEnabled"))
        {
            isServerLogsEnabled = prefs.ValueBool("isServerLogsEnabled");
            DT.Log("isServerLogsEnabled set to " + isServerLogsEnabled.ToString());
        }

        if (prefs.Contains("TanksSlots"))
        {
            hangarSlots = prefs.ValueInt("TanksSlots");
        }

        NotifyAboutChanges(true, hangarSlots);

        Dictionary<string, object> units = data.ExtractOrDefault<Dictionary<string, object>>("Tanks");
        int blocId = -1;
        if (data.ContainsKey("BlocId"))
        {
            blocId = data.ExtractOrDefault<int>("BlocId");
        }

        StaticType.Profile.Instance().Reaction(XD.Message.ServerDataReceived, ServerDataType.ProfileData, units, (int)dailyBonusIndex, (bool)dailyBonusIsObtained, (int)currentVehicle, blocId);        
        return true;
    }

    public static void ApplyProfileChanges(Dictionary<string, object> data)
    {
        var prefs = new JsonPrefs(data);
        bool haveApplyedChanges = false;

        if (data.ContainsKey("TanksSlots"))
        {
            hangarSlots = prefs.ValueInt("TanksSlots");
            haveApplyedChanges = true;
        }

        if (data.ContainsKey("Gold"))
        {
            gold = prefs.ValueInt("Gold");
            haveApplyedChanges = true;
        }

        if (data.ContainsKey("Silver"))
        {
            silver = prefs.ValueInt("Silver");
            haveApplyedChanges = true;
        }

        if (data.ContainsKey("NickName"))
        {
            PlayerName = prefs.ValueString("NickName", PlayerName);
            haveApplyedChanges = true;
        }

        if (data.ContainsKey("Email"))
        {
            playerEmail = prefs.ValueString("Email", playerEmail);
            playerEmailConfirmed = prefs.ValueBool ("EmailConfirmed", playerEmailConfirmed);
            haveApplyedChanges = true;
        }

        if (data.ContainsKey("Experience"))
        {
            Experience = prefs.ValueInt("Experience", Experience);
            haveApplyedChanges = true;
        }

        if (data.ContainsKey("ExperiencePoints"))
        {
            ExperiencePoints = prefs.ValueInt("ExperiencePoints", ExperiencePoints);
            haveApplyedChanges = true;
        }

        if (data.ContainsKey("LastVisit"))
        {
            lastVisit = prefs.ValueDouble("LastVisit", lastVisit);
            haveApplyedChanges = true;
        }

        if (data.ContainsKey("CurrentTank"))
        {
            CurrentUnit = prefs.ValueInt("CurrentTank", CurrentUnit);
            haveApplyedChanges = true;
        }

        if (data.ContainsKey("DoubleExpTanks"))
        {
            GetDoubleExpVehiclesList(prefs.ValueObjectList("DoubleExpTanks"));
            haveApplyedChanges = true;
        }

        if (data.ContainsKey("CompletedTutorials"))
        {
            GetCompletedTutorialsFromList(data.ExtractOrDefault<List<object>>("CompletedTutorials"));
            haveApplyedChanges = true;
        }

        if (data.ContainsKey("Kits"))
        {
            ParseKits(prefs.ValueObjectDict("Kits"));
            haveApplyedChanges = true;
        }

        if (data.ContainsKey("InitialAcceleration"))
        {
            var dict = prefs.ValueObjectDict("InitialAcceleration");
            initialAcceleration = Vector3.one * ((float)dict["x"] + (float)dict["y"] + (float)dict["z"]);
        }

        if (data.ContainsKey("Tanks"))
        {
            GetVehiclesFromDict(prefs.ValueObjectDict("Tanks"));
            haveApplyedChanges = true;
        }

        if (data.ContainsKey("VipExpiration"))
        {
            VipExpirationDate = prefs.ValueInt("VipExpiration", 0);
            haveApplyedChanges = true;
        }

        if (data.ContainsKey("IsPlayerVip"))
        {
            IsPlayerVip = prefs.ValueBool("IsPlayerVip");
            haveApplyedChanges = true;
        }

        if (data.ContainsKey("XDevsOffers"))
        {
            xdevsOffers = new Dictionary<string, XDevsOffersStatus>();
            foreach (var offerState in prefs.ValueObjectDict("XDevsOffers"))
            {
                try
                {
                    xdevsOffers[offerState.Key] = (XDevsOffersStatus)Enum.Parse(typeof(XDevsOffersStatus), prefs.ValueString("XDevsOffers/" + offerState.Key + "/status"), true);
                }
                catch
                {
                    Debug.LogError("Can't parse offer state for game " + offerState.Key);
                }
            }
            haveApplyedChanges = true;
        }

        if (data.ContainsKey("Quests"))
        {
            QuestsInfo.FromDictionary(prefs.ValueObjectList("Quests"));
            haveApplyedChanges = true;
        }

        if (data.ContainsKey("UnreadMessages"))
        {
            unreadMessages = prefs.ValueInt("UnreadMessages");
            haveApplyedChanges = true;
        }

        // Обновить профиль, но дергать изменения нет необходимости
        if (data.ContainsKey("NickEntered"))
        {
            nickEntered = prefs.ValueBool("NickEntered");
            /*if (Settings.Instance)
                Settings.Instance.UpdateChangeNickPrice();*/
        }
        if (data.ContainsKey("NickRejected"))
        {
            nickRejected = prefs.ValueBool("NickRejected");
        }
        if (data.ContainsKey("DaysInARow"))
        {
            daysInARow = prefs.ValueInt("DaysInARow", 0);
        }
        if (data.ContainsKey("RatedForBonus"))
        {
            ratedForBonus = prefs.ValueBool("RatedForBonus");
        }

        if (data.ContainsKey("IsGameRated"))
        {
            isGameRated = prefs.ValueBool("IsGameRated");
        }

        if (data.ContainsKey("NextDayServerTime"))
        {
            nextDayServerTime = prefs.ValueDouble("NextDayServerTime");
        }

        if (data.ContainsKey("DailyBonusIsObtained"))
        {
            dailyBonusIsObtained = prefs.ValueBool("DailyBonusIsObtained");
        }

        if (data.ContainsKey("GoldKitAwardIsObtained"))
        {
            goldKitAwardIsObtained = prefs.ValueBool("GoldKitAwardIsObtained");
        }

        if (data.ContainsKey("GoldKitAwardVal"))
        {
            goldKitAwardVal = prefs.ValueInt("GoldKitAwardVal");
        }

        if (data.ContainsKey("LastLevelUpAward"))
        {
            lastLevelUpAward = prefs.ValueInt("LastLevelUpAward", 1);
        }
        if (haveApplyedChanges)
        {
            NotifyAboutChanges(data.ContainsKey("Tanks") ||
                                data.ContainsKey("CurrentTank") ||
                                data.ContainsKey("DoubleExpTanks"), hangarSlots);
        }

        Dictionary<string, object> units = data.ExtractOrDefault<Dictionary<string, object>>("Tanks");
        int blocId = -1;
        if (data.ContainsKey("BlocId"))
        {
            blocId = data.ExtractOrDefault<int>("BlocId");
        }

        StaticType.Profile.Instance().Reaction(XD.Message.ServerDataReceived, ServerDataType.ProfileData, units, (int)dailyBonusIndex, (bool)dailyBonusIsObtained, (int)currentVehicle, blocId);
        SetGameMode(prefs);

        if (data.ContainsKey("Notifications"))
        {
            StaticType.MailBox.Instance().Reaction(XD.Message.ServerDataReceived, ServerDataType.Notifications, data.ExtractOrDefault<List<object>>("Notifications"));
        }

    }

    private static void NotifyAboutChanges(bool tanksChanged, int hangarSlots)
    {
        if (languageIndex <= 0)
        {
            languageIndex = (int)StaticType.Localization.Instance<ILocalization>().CurrentLanguage;
        }

        CalcLevel();

        Dispatcher.Send(EventId.ProfileMoneyChange, new EventInfo_II(gold, silver));

        if (tanksChanged)
        {
            Dispatcher.Send(EventId.ShopInfoLoadedFromServer, new EventInfo_SimpleEvent());
        }

        if (useDebugPanelForLogging)
        {
            DT.useDebugPanelForLogging = true;
            XdevsSplashScreen.InstantiateDebugPanel();
        }

        StaticType.Profile.Instance().Reaction(XD.Message.ProfileInited, hangarSlots);
        StaticType.GamesContainer.Instance().Reaction(XD.Message.ProfileInited, hangarSlots);
        StaticType.NotificationDispatcher.Instance().Reaction(XD.Message.ProfileInited);
        StaticType.Bank.Instance().Reaction(XD.Message.ProfileInited);
    }

    public static int ExperienceForLevel(int _level)
    {
        if (_level < 2)
        {
            return 0;
        }

        return (int)((Math.Pow(2, (double)_level / 2 - 1)) * 1000) - 500;
    }

    public static int LevelForExperience(int _experience)
    {
        return (int)(2.0 * (1.0 + Math.Log((double)(_experience + 500) / 1000, 2.0)));
    }

    public static string Version
    {
        get
        {
            return version;
        }
        set
        {
            version = value;
        }
    }

    public static bool ImportantUpdate
    {
        get
        {
            return importantUpdate;
        }
        set
        {
            importantUpdate = value;
        }
    }

    // stackoverflow.com/a/3294698/162671
    /*	public static uint SwapEndianness(ulong x)
        {
            return (uint)(((x & 0x000000ff) << 24) +
                           ((x & 0x0000ff00) << 8) +
                           ((x & 0x00ff0000) >> 8) +
                           ((x & 0xff000000) >> 24));
        }*/

    private static Dictionary<string, object> UserTanksDict()
    {
        var dict = new Dictionary<string, object>(vehicleUpgrades.Count);
        foreach (var el in vehicleUpgrades)
        {
            dict[el.Key.ToString()] = el.Value.ToDictionary();
        }
        return dict;
    }

    /// <summary>
    /// get all tanks user owned
    /// </summary>
    /// <param name="dict"></param>
    private static void GetVehiclesFromDict(Dictionary<string, object> dict)
    {
        if (dict == null || dict.Count == 0)
        {
            DT.LogError("No user vehicles loaded");
            return;
        }
        vehicleUpgrades = new Dictionary<int, VehicleUpgrades>();
        foreach (var el in dict)
        {
            vehicleUpgrades[Convert.ToInt32(el.Key)] = VehicleUpgrades.FromDictionary(el.Value as Dictionary<string, object>);
        }
    }

    private static void GetDoubleExpVehiclesList(IEnumerable<object> list)
    {
        if (list == null)
        {
            DT.LogError("No doubleExp vehicles loaded");
            return;
        }

        doubleExpVehicles.Clear();
        foreach (var vehicle in list)
        {
            doubleExpVehicles.Add(Convert.ToInt32(vehicle));
        }
    }

    private static void GetCompletedTutorialsFromList(List<object> list)
    {
        if (list == null)
        {
            Debug.LogError("List Tutorials is null!");
            list = new List<object>();
        }

        if (list.Count == 0)
        {
            accomplishedTutorials = new List<int>() { -1 };
            return;
        }

        accomplishedTutorials = new List<int>(list.Count);
        foreach (object index in list)
        {
            accomplishedTutorials.Add(Convert.ToInt32(index));
        }
    }

    private static void GetSocialActivity(IEnumerable<object> list)
    {
        if (list == null)
        {
            return;
        }

        foreach (var activity in list)
        {
            try
            {
                socialActivity.Add((SocialAction)Enum.Parse(typeof(SocialAction), (string)activity));
            }
            catch (Exception)
            {
                Debug.Log("cant parse socialactivity: " + activity);
            }
        }
    }

    public static void LoadDefaults()
    {
        enterForFirstTime = true;
        ratedForBonus = false;
        gold = START_GOLD;
        silver = START_SILVER;
        experience = 0;
        CalcLevel();
        vehicleUpgrades = new Dictionary<int, VehicleUpgrades>();
        doubleExpVehicles = new List<int>();
        CurrentUnit = 1;
        dailyBonusIndex = 0;
        tutorialIndex = (int)Tutorials.BattleTutorial;
        launchesCount = 0;
        nickEntered = false;
        daysInARow = 0;
        //vehicles.Add(1, new VehicleUpgrades(HangarController.Instance.defaultVehicleInfo));
        doubleExpVehicles.Add(CurrentUnit);
        lastProfileSaveTimestamp = 0;
        VipExpirationDate = (int)MiscTools.unixOrigin;
        avatarOption = (int)AvatarOption.showEverything;
        controlOption = (int)ControlOption.joystick;
        isHideMyFlag = false;
        isSliderControl = true;
        isVoiceDisabled = false;
        accomplishedTutorials = new List<int>() { -1 };
    }

    private static void ParseKits(Dictionary<string, object> kitsDict)
    {
        if (kitsDict == null || GameData.bankKits == null || GameData.bankKits.Count == 0)
            return;
        JsonPrefs kitsDictPrefs = new JsonPrefs(kitsDict);
        if (kitsDictPrefs == null)
            return;
        foreach (KeyValuePair<string, object> kitTypePair in kitsDict)
        {
            BankKits.Type kitType;
            if (!HelpTools.TryParseToEnum(kitTypePair.Key, out kitType))
                continue;
            Dictionary<string, object> kitDict = kitsDictPrefs.ValueObjectDict(kitTypePair.Key);
            foreach (KeyValuePair<string, object> kitPair in kitDict)
            {
                if (!GameData.bankKits.ContainsKey(kitType) || !GameData.bankKits[kitType].ContainsKey(kitPair.Key))
                    continue;
                var kitPrefs = new JsonPrefs(kitPair.Value);
                GameData.bankKits[kitType][kitPair.Key].startTime = kitPrefs.ValueDouble("availableFrom");
                GameData.bankKits[kitType][kitPair.Key].endTime = kitPrefs.ValueDouble("availableTo");
                GameData.bankKits[kitType][kitPair.Key].duration = (int)(GameData.bankKits[kitType][kitPair.Key].endTime - GameData.bankKits[kitType][kitPair.Key].startTime);
                GameData.bankKits[kitType][kitPair.Key].needToShow = kitPrefs.ValueBool("needToShow");
            }
        }
    }

    private static void CalcLevel()
    {
        level = Mathf.Clamp(LevelForExperience(Experience), 1, 50);
        prevExperience = level > 1 ? ExperienceForLevel(level) : 0;
        nextExperience = ExperienceForLevel(level + 1);
    }

    private static void AfterErrorInforming()
    {
        HangarController.QuitGame();
    }

    public static void HinduBugFix()
    {
        foreach (VehicleUpgrades upgrade in vehicleUpgrades.Values)
        {
            if (Math.Abs(upgrade.moduleReadyTime - GameData.CurrentTimeStamp) > MAX_TIME_INTERVAL)
                upgrade.moduleReadyTime = GameData.CurrentTimeStamp +
                                          TankModuleInfos.Instance.inventionTime[upgrade.GetModuleLevel(upgrade.awaitedModule) + 1] * 60;
        }
    }

    private static void SetGameMode(JsonPrefs prefs)
    {
        if (prefs.Contains("GameMode"))
        {
            int tempGameMode = prefs.ValueInt("GameMode");
            GameData.Mode = Enum.IsDefined(typeof(GameData.GameMode), tempGameMode) ? (GameData.GameMode)tempGameMode : GameData.DefaultGameMode;
            //DT.LogError("ProfileInfo. Loaded GameMode = {0}", parsedValue);
        }
    }
}
