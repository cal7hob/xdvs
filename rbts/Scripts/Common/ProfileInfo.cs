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

public static class ProfileInfo
{
    private const int START_MAX_FUEL = 10;
    private const int START_FUEL = START_MAX_FUEL;
    private const int START_SILVER = 5000;
    private const int START_GOLD = 0;
    public const int CHAT_BAN_1HOUR = 3600;
    private const float MAX_TIME_INTERVAL = 3600 * 24 * 14;

    private static ObscuredInt level = 1;
    private static ObscuredInt experience = 0;
    private static int nextExperience = 320;
    private static int prevExperience = 240;
    private static ObscuredInt silver = 0;
    private static ObscuredInt gold = 0;
    private static ObscuredDouble fuel = 0;
    private static ObscuredInt maxFuel = 0;
    private static ObscuredInt goldForFullFuelTank = 3;
    private static string countryCode;
    private static bool importantUpdate;
    private static string version;
    private static string marketURL;
    private static ObscuredInt playerPrivilege = 0;

    private static ObscuredString playerName = "Guest";
    private static ObscuredString playerEmail = "";
    public static ObscuredInt profileId = 0;
    private static ObscuredBool isPlayerVip = false;
    private static ObscuredBool isCheater = false;
    private static ObscuredInt vipExpirationDate;
    private static ObscuredFloat vipExpRate = 1.5f;
    private static ObscuredFloat vipSilverRate = 1.2f;
    private static Clan clan;

    public static ObscuredInt currentVehicle = 0;
    public static ObscuredInt dailyBonusIndex;
    public static ObscuredInt dailyBonusDaysCount;
    public static ObscuredBool goldKitAwardIsObtained = true;//По умолчанию считаем что награда дана, в новый день сервер пришлет false и покажется окно награды, после того как пользователь возьмет награду - сервер пришлет true
    public static ObscuredInt goldKitAwardVal;
    public static ObscuredInt launchesCount;
    public static ObscuredDouble nextDayServerTime;
    public static ObscuredBool dailyBonusIsObtained;
    public static ObscuredDouble lastVisit;
    //public static ObscuredDouble lastFriendsInvite = 0;
    public static ObscuredLong lastProfileSaveTimestamp = 0;
    #region AppGuid
    public static string AppGuid {
        get { return appGUID; }
        set {
            appGUID = value;
            PlayerPrefs.SetString (DeviceGuidKey, appGUID);
            PlayerPrefs.Save ();
        }
    }
    private static string appGUID = "";
    #endregion
    public static ObscuredBool ratedForBonus;
    public static ObscuredBool nickEntered;
    public static ObscuredInt daysInARow;
    public static Dictionary<int, ObscuredInt> consumableInventory = new Dictionary<int, ObscuredInt>();
    public static List<int> consumableInventoryPanelItems = new List<int>();//индекс -> слот, элемент -> ид_расходки

    /// <summary>
    /// Входит ли страна игрока в число стран со специальными правилами матчмейкинга
    /// </summary>
    public static bool fromSpecialCountry;
    public static Dictionary<int, VehicleUpgrades> vehicleUpgrades = new Dictionary<int, VehicleUpgrades>(10);
    public static List<int> doubleExpVehicles = new List<int>();
    public static List<SocialAction> socialActivity = new List<SocialAction>();
    public static bool enterForFirstTime = false;
    public static bool nickRejected = false;
    public static Dictionary<string, object> weeklyAwardsDict = new Dictionary<string, object>();
    public static Dictionary<string, object> clanInfoDict = new Dictionary<string, object>();
    public static Dictionary<string, object> adsDict = new Dictionary<string, object>();
    public static ObscuredBool useDebugPanelForLogging = false;
    public static ObscuredBool isServerLogsEnabled = false;
    public static Dictionary<Tutorials, bool> accomplishedTutorials;
    public static Vector3 initialAcceleration = Vector3.zero;

    public static Dictionary<string, XDevsOffersStates> xdevsOffers = new Dictionary<string, XDevsOffersStates>();

    // Social settings
    public static ObscuredBool isSocialActivated = false;
    public static bool isFuelForInviteAvailable = false;
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
    public static bool isPushForFuel = false;
    public static bool isVoiceDisabled;
    public static int avatarOption;
    public static int controlOption;
    public static int languageIndex = -1;

    #region Bank offers limits
    
    private static bool isGoldDiscountLimitReached = false;
    private static bool isSilverDiscountLimitReached = false;
    private static bool isVipDiscountLimitReached = false;

    public static bool IsGoldDiscountLimitReached { get { return isGoldDiscountLimitReached; } }
    public static bool IsSilverDiscountLimitReached { get { return isSilverDiscountLimitReached; } }
    public static bool IsVipDiscountLimitReached { get { return isVipDiscountLimitReached; } }

    #endregion

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

    public static string notificationsJsonObjectName = "Notifications";
    public static List<object> notificationsList = new List<object>();

    public static int FuelRequired { get; set; }

    #region Save profile once per request
    /**********************************************************************
     * Контроль только одного активного сохранения профиля на сервер за раз
     */
    private static bool m_isSaveToServerActive = false;
    private static bool m_isSaveToServerRequired = false;
    private static List<Action<Http.Response, bool>> m_awaitedSaveCallbacks = new List<Action<Http.Response, bool>>();
    #endregion

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
        Silver = 0,
        Gold
    }

    public enum XDevsOffersStates
    {
        Clicked,
        Installed
    }

    public static PlayerPrivileges PlayerPrivilege
    {
        get { return (PlayerPrivileges)Convert.ToInt32(playerPrivilege); }
    }

    public static AvatarOption AvatarOption
    {
        get { return (AvatarOption)avatarOption; }
    }

    public static ControlOption ControlOption
    {
        get { return (ControlOption)controlOption; }
    }

    public static bool IsVoiceDisablingAvailable
    {
        get { return Localizer.Language == Localizer.LocalizationLanguage.Russian ||
                     Localizer.Language == Localizer.LocalizationLanguage.English; }
    }

    public static bool IsBattleTutorial
    {
        get { return TutorialIndex == (int) Tutorials.battleTutorial; }
    }

    public static bool IsBattleTutorialCompleted
    {
        get
        {
            return accomplishedTutorials[Tutorials.battleTutorial];
        }
    }

    public static bool IsEnterNickTutorialCompleted
    {
        get
        {
            return accomplishedTutorials[Tutorials.enterName];
        }
    }

    public static int RecentlyFinishedTutorialIndex { get { return TutorialIndex - 1; } }

    public static int TutorialIndex 
    {
        get {
            var tutorialIndex = 0;
            foreach (var accomplished in accomplishedTutorials.Values) {
                if (!accomplished) {
                    break;
                }
                tutorialIndex++;
            }

            return tutorialIndex;
        }
    }

    [Serializable]
    public class Price
    {
        public ObscuredInt value;
        public PriceCurrency currency;

        private static Dictionary<Interface, Dictionary<PriceCurrency, Color>> moneyColors = new Dictionary<Interface, Dictionary<PriceCurrency, Color>>()
        {
            {Interface.BattleOfHelicopters, new Dictionary<PriceCurrency, Color>()
                {
                    {PriceCurrency.Gold, new Color(1, 0.64f, 0.2f)},//255,163,56
                    {PriceCurrency.Silver, Color.white},
                }
            },
            {Interface.Armada, new Dictionary<PriceCurrency, Color>()
                {
                    {PriceCurrency.Gold, new Color32(210, 135, 55, 255)},
                    {PriceCurrency.Silver, new Color32(130, 175, 175, 255)},//82afaf
                }
            },
            {Interface.BattleOfWarplanes, new Dictionary<PriceCurrency, Color>()
                {
                    {PriceCurrency.Gold, new Color32(255, 194, 95, 255)},
                    {PriceCurrency.Silver, new Color32(206, 206, 206, 255)},
                }
            },
        };

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
        public string SpriteName { get { return currency.ToString().ToLower();} }

        /// <summary>
        /// value.ToString("N0", GameData.instance.cultureInfo.NumberFormat);
        /// </summary>
        public string ValueFormat_N0 { get { return value.ToString("N0", GameData.instance.cultureInfo.NumberFormat);} }

        public static implicit operator bool(Price price)
        {
            return (price != null && !HelpTools.Approximately(price.value, 0));
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


        public string LocalizedValue { get { return value.ToString("N0", GameData.instance.cultureInfo.NumberFormat); } }

        public Color MoneySpecificColor
        {
            get
            {
                if (!moneyColors.ContainsKey(GameData.CurInterface) || !moneyColors[GameData.CurInterface].ContainsKey(currency))
                    return Color.white;
                return moneyColors[GameData.CurInterface][currency];
            }
            
        }

        public string GetBankSpriteByPriceRange()
        {
            if (currency == ProfileInfo.PriceCurrency.Gold)
            {
                if(value >= 0 && value <= 5) return "gold_0";
                if (value >= 6 && value <= 10) return "gold_1";
                if (value >= 11 && value <= 15) return "gold_2";
                if (value >= 16 && value <= 20) return "gold_3";
                if (value >= 21 && value <= 25) return "gold_4";
                if (value >= 26) return "gold_5";
            }
            else
            {
                if (value >= 0 && value <= 1000) return "silver_0";
                if (value >= 1001 && value <= 2000) return "silver_1";
                if (value >= 2001 && value <= 3000) return "silver_2";
                if (value >= 3001 && value <= 4000) return "silver_3";
                if (value >= 4001 && value <= 5000) return "silver_4";
                if (value >= 5001) return "gold_5";
            }
            return "gold_0";
        }

        /// <summary>
        /// Set MoneySpecificColor To label if inlineStyling option enabled.
        /// If customAlpha == -1, use current label alpha
        /// </summary>
        public void SetMoneySpecificColorIfCan(tk2dTextMesh lbl, float customAlpha = -1)
        {
            if(lbl == null)
            {
                DT.LogError("Cant SetMoneySpecificColor. lbl is NULL");
                return;
            }

            if(lbl.inlineStyling)
            {
                float alpha = Mathf.Approximately(customAlpha, -1) ? lbl.color.a : customAlpha;
                lbl.color = new Color(1,1,1, alpha);
                Color c = MoneySpecificColor;
                lbl.text = new Color(c.r, c.g, c.b).To2DToolKitColorFormatString() + lbl.text;
            }
        }
    }

    public static ObscuredString PlayerName
    {
        get { return playerName; }
        set
        {
            playerName = value;
            Messenger.Send(EventId.NickNameChanged, new EventInfo_SimpleEvent());
        }
    }

    public static ObscuredString PlayerEmail
    {
        get { return playerEmail; }
    }

    public static string CountryCode
    {
        get { return countryCode; }
        set
        {
            if (String.IsNullOrEmpty(countryCode))
            {
                countryCode = value;
            }
        }
    }

    public static string FlagSprite
    {
        get { return (string.IsNullOrEmpty(CountryCode) || CountryCode.ToLower() == "unknown") ? GameData.UNKNOWN_FLAG_NAME : CountryCode.ToLower(); }
    }

    public static bool WasInBattle
    {
        get
        {
            if (!PlayerPrefs.HasKey("WasInBattle"))
                return WasInBattle = BattleStatisticsManager.OverallBattleStats["BattlesCount"] > 0;

            return Convert.ToBoolean(PlayerPrefs.GetInt("WasInBattle"));
        }
        set
        {
            PlayerPrefs.SetInt("WasInBattle", Convert.ToInt32(value));
        }
    }

    public static int GoldForFullFuelTank
    {
        get { return goldForFullFuelTank; }
    }

    public static int Level
    {
        get { return level; }
    }

    public static string MarketURL
    {
        get { return marketURL; }
        set { marketURL = value; }
    }

    public static int Experience
    {
        get { return experience; }
        set
        {
            experience = value;
            int oldLevel = Level;
            CalcLevel();
            //Пока такой проверки достаточно, но вообще надо что то получше, чтобы отсеить первую отсылку после инициализации переменных
            if(Level != oldLevel)
            {
                GameData.UpdateAvailableMaps();
                Messenger.Send(EventId.PlayerLevelChanged,new EventInfo_SimpleEvent());
            }
        }
    }

    public static int CurrentVehicle
    {
        get { return currentVehicle; }
        set { currentVehicle = value; }
    }

    public static int PrevExperience
    {
        get { return prevExperience; }
    }

    public static int NextExperience
    {
        get { return nextExperience; }
    }

    public static int Silver
    {
        get { return silver; }
    }

    public static int Gold
    {
        get { return gold; }
    }

    public static double Fuel
    {
        get { return fuel; }
        set
        {
            fuel = Mathf.Clamp((float)value, 0, maxFuel);
            Messenger.Send(EventId.FuelUpdated, new EventInfo_I((int)fuel));
        }
    }

    public static int MaxFuel
    {
        get { return maxFuel; }
        set
        {
            maxFuel = Mathf.Clamp(value, 1, 20);
            fuel = Mathf.Clamp((float)fuel, 0, maxFuel);
            Messenger.Send(EventId.MaxFuelUpdated, new EventInfo_I(maxFuel));
        }
    }

    public static bool IsFullTank
    {
        get { return Fuel >= MaxFuel; }
    }

    public static bool IsPlayerVip
    {
        get { return isPlayerVip; }
        set
        {
            if (value == isPlayerVip) return;
            isPlayerVip = value;
            Messenger.Send(EventId.VipStatusUpdated, new EventInfo_B(isPlayerVip));
            // update fuel cells
            MaxFuel = isPlayerVip
                ? GameData.STANDART_FUEL_CAN_AMOUNT + GameData.EXTRA_FUEL_CAN_AMOUNT
                : GameData.STANDART_FUEL_CAN_AMOUNT;
        }
    }

    public static bool IsCheater
    {
        get { return isCheater; }
        set { isCheater = value; }
    }

    public static bool IsNewbie
    {
        get
        {
            return BattleStatisticsManager.OverallBattleStats["BattlesCount"] < GameManager.NEWBIE_BATTLES_AMOUNT &&
                   currentVehicle == GameManager.NEWBIE_VEHICLE_ID;
        }
    }

    public static bool LastSessionVipStatus
    {
        get; set;
    }

    public static int VipExpirationDate
    {
        get { return vipExpirationDate; }
        set
        {
            vipExpirationDate = value;
            IsPlayerVip = (int)GameData.CorrectedCurrentTimeStamp > Clock.YEAR_SECONDS
                          && vipExpirationDate > (int)GameData.CorrectedCurrentTimeStamp;
        }
    }


    public static float VipExpRate
    {
        get { return vipExpRate; }
    }


    public static float VipSilverRate
    {
        get { return vipSilverRate; }
    }

    public static Price SocialActivationReward
    {
        get
        {
            return GameData.socialActivationReward;
        }
    }

    public static Clan Clan
    {
        get { return clan; }
        set
        {
            if (value == null && clan == null)
                return;

            if (clan != null && clan.Equals(value))
                return;

            clan = value;
            Messenger.Send(EventId.ClanChanged, null);
        }
    }

    public static string DeviceGuidKey { get { return "GUID_" + Http.Manager.Instance ().Region; } }

    static ProfileInfo () {
        accomplishedTutorials = new Dictionary<Tutorials, bool>(new EnumExtensions.TutorialsComparer());
        foreach (Tutorials t in Enum.GetValues (typeof (Tutorials))) {
            accomplishedTutorials[t] = false;
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

        Messenger.Send(EventId.ProfileMoneyChange, new EventInfo_II(gold, silver));
    }

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
        Debug.Log (dataToSave);

        Request request = Manager.Instance().CreateRequest("/player/save");
        request.Form.AddField("data", dataToSave);

        long timestampForSave = (long)GameData.CurrentTimeStamp;

        request.Form.AddField("timestamp", (timestampForSave).ToString());
        request.Form.AddField("lastTimestamp", lastProfileSaveTimestamp.ToString());

        //Debug.Log ("lastTimestamp=" + lastProfileSaveTimestamp.ToString () + ", tsForSave=" + tsForSave);

        Manager.StartAsyncRequest(
            request,
            delegate(Response result)
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
            delegate(Response result)
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

    public static bool LoadProfile (object profileData)
    {
        var receivedProfileData = profileData as Dictionary<string, object>;
        if (receivedProfileData == null) {
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

        profileId = Convert.ToInt32(receivedProfileData["playerId"]); // maybe move it to ApplyLoadedData?
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

#if UNITY_EDITOR
        appGUID = GameData.GetForcedGUID();
        if (appGUID != null)
        {
            LoadDefaults();
            yield break;
        }
#endif
        if (PlayerPrefs.HasKey (DeviceGuidKey)) {
            appGUID = PlayerPrefs.GetString (DeviceGuidKey);
        }

        // Отключаем в вебверсии загрузку профиля
        LoadDefaults ();
    }

    public static string PrepareForSave()
    {
        var dict = new Dictionary<string, object>();

        dict["Gold"] = (int)gold;
        dict["Silver"] = (int)silver;
        dict["Fuel"] = (double)fuel;
        dict["MaxFuel"] = (int)maxFuel;
//        dict["LastVisit"] = (double)lastVisit;
//        dict["LastFriendsInvite"] = (double)lastFriendsInvite;
        dict["DoubleExpTanks"] = doubleExpVehicles;
        dict["CompletedTutorials"] = accomplishedTutorials;
        dict["InitialAcceleration"] = new Dictionary<string, float>()
        {
            {"x", Settings.InitialAcceleration.x},
            {"y", Settings.InitialAcceleration.y},
            {"z", Settings.InitialAcceleration.z}
        };

        dict["OverallBattleStatisticsJson"] = MiniJSON.Json.Serialize(BattleStatisticsManager.OverallBattleStats);
        dict["LaunchesCount"] = (int)launchesCount;
        dict["TutorialIndex"] = (int)TutorialIndex;
        dict["Achievements"] = savedAchievements;
        dict["ConsumableInventoryPanelItems"] = consumableInventoryPanelItems;

        // Параметры из окна настроек игры.
        dict["Invert"] = isInvert;
        dict["SliderControl"] = isSliderControl;
        dict["FireOnDoubleTap"] = isFireOnDoubleTap;
        dict["HideMyFlag"] = isHideMyFlag;
        dict["PushForDailyBonus"] = isPushForDailyBonus;
        dict["PushForUpgrade"] = isPushForUpgrade;
        dict["PushForFuel"] = isPushForFuel;
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

        Experience = prefs.ValueInt("Experience", -1);
        if (Experience < 0)
        {
            Debug.LogError("Can't read Experience value");
            return false;
        }

        fuel = prefs.ValueDouble("Fuel", -1);
        if (fuel < 0)
        {
            Debug.LogError("Can't read Fuel value");
            return false;
        }

        maxFuel = prefs.ValueInt("MaxFuel", -1);
        if (maxFuel < 0)
        {
            Debug.LogError("Can't read MaxFuel value");
            return false;
        }

        lastVisit = prefs.ValueDouble("LastVisit", -1);
        if (lastVisit < 0)
        {
            Debug.LogError("Can't read LastVisit value");
            return false;
        }

        isFuelForInviteAvailable = prefs.ValueBool("IsFuelForInviteAvailable", true);

        CurrentVehicle = prefs.ValueInt("CurrentTank", -1);
        if (CurrentVehicle < 0)
        {
            Debug.LogError("Can't read CurrentTank value");
            return false;
        }

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

        ParseKits(prefs.ValueObjectDict("Kits", null));

        if (prefs.Contains("InitialAcceleration"))
        {
            initialAcceleration = Vector3.right   * prefs.ValueFloat ("InitialAcceleration/x") +
                                  Vector3.up      * prefs.ValueFloat ("InitialAcceleration/y") +
                                  Vector3.forward * prefs.ValueFloat ("InitialAcceleration/z");
        }

        GetSocialActivity(prefs.ValueObjectList("SocialActivity"));

        lastLevelUpAward = prefs.ValueInt("LastLevelUpAward", 1);
        savedAchievements = prefs.ValueObjectDict("Achievements");
        QuestsInfo.FromDictionary(prefs.ValueObjectList("Quests"));
        SetGameMode(prefs);
        BattleStatisticsManager.SetOverallBattleStatsDictionary(prefs.ValueString("OverallBattleStatisticsJson", ""));

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
        isPushForFuel = prefs.ValueBool("PushForFuel", true);
        isVoiceDisabled = prefs.ValueBool("VoiceDisabled", false);
        avatarOption = prefs.ValueInt("AvatarOption");
        controlOption = prefs.ValueInt("ControlOption");

#if LANG_CHINESE_ONLY
        languageIndex = (int)Localizer.LocalizationLanguage.Chinese;
#else
        languageIndex = prefs.ValueInt("LanguageIndex", (int)Localizer.GetDefaultLanguage());
#endif
        if (languageIndex == 0) {
            languageIndex = (int)Localizer.GetDefaultLanguage ();
        }
        Localizer.Language = (Localizer.LocalizationLanguage)languageIndex;


        launchesCount = prefs.ValueInt("LaunchesCount", 1);

        nextDayServerTime = prefs.ValueDouble("NextDayServerTime");

        dailyBonusIsObtained = prefs.ValueBool("DailyBonusIsObtained");
        dailyBonusIndex = prefs.ValueInt("DailyBonusIndex", 0);

        goldKitAwardIsObtained = prefs.ValueBool("GoldKitAwardIsObtained", true);
        goldKitAwardVal = prefs.ValueInt("GoldKitAwardVal");

        isSocialActivated = prefs.ValueBool("IsSocialActivated", false);
        lastMapId = prefs.ValueInt("LastMapId");
        nickEntered = prefs.ValueBool("NickEntered", true);
        nickRejected = prefs.ValueBool("NickRejected", true);
        daysInARow = prefs.ValueInt("DaysInARow", 0);
        // Rate This Game
        ratedForBonus = prefs.ValueBool("RatedForBonus", false);
        isGameRated = prefs.ValueBool("IsGameRated", false);
        unreadMessages = prefs.ValueInt("UnreadMessages");

        xdevsOffers = new Dictionary<string, XDevsOffersStates>();
        if (prefs.Contains("XDevsOffers"))
        {
            foreach (var offerState in prefs.ValueObjectDict ("XDevsOffers"))
            {
                try {
                    xdevsOffers[offerState.Key] = (XDevsOffersStates)Enum.Parse(typeof(XDevsOffersStates), prefs.ValueString("XDevsOffers/" + offerState.Key + "/status"), true);
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

        if (prefs.Contains("Consumables"))
        {
            LoadInventory(prefs.ValueObjectDict("Consumables"));
        }

        if (prefs.Contains("ConsumableInventoryPanelItems"))
            LoadConsumableInventoryPanelItemsFromDict(prefs.ValueObjectList("ConsumableInventoryPanelItems"));

        if (prefs.Contains("weeklyAwards"))
        {
            weeklyAwardsDict = prefs.ValueObjectDict("weeklyAwards");
        }

        // Player's clan
        if (prefs.Contains("ClanInfo"))
        {
            clanInfoDict = prefs.ValueObjectDict("ClanInfo");
        }

        #region Notifications

        if (prefs.Contains(notificationsJsonObjectName))
        {
            notificationsList = prefs.ValueObjectList(notificationsJsonObjectName);
        }

        #endregion

        GetVehiclesFromDict(l_userTanksDict);

        lastProfileSaveTimestamp = timestamp;
        DT.Log("Profile data loaded, lastProfileSaveTimestamp=" + lastProfileSaveTimestamp);

        if (prefs.Contains ("useDebugPanelForLogging"))
        {
            useDebugPanelForLogging = prefs.ValueBool ("useDebugPanelForLogging");
            DT.Log("useDebugPanelForLogging set to " + useDebugPanelForLogging);
        }
        if (prefs.Contains ("isServerLogsEnabled"))
        {
            isServerLogsEnabled = prefs.ValueBool ("isServerLogsEnabled");
            DT.Log("isServerLogsEnabled set to " + isServerLogsEnabled);
        }
        
        if (prefs.Contains("Ads"))
        {
            adsDict = prefs.ValueObjectDict("Ads");
        }

        LoadTutorials (prefs.ValueObjectDict ("CompletedTutorials"));

        #region Bank Offers limits
        
        isGoldDiscountLimitReached   = prefs.ValueBool ("IsGoldDiscountLimitReached", false);
        isSilverDiscountLimitReached = prefs.ValueBool ("IsSilverDiscountLimitReached", false);
        isVipDiscountLimitReached    = prefs.ValueBool ("IsVipDiscountLimitReached", false);
        
        #endregion

        NotifyAboutChanges (tanksChanged: true);

        return true;
    }

    public static void ApplyProfileChanges(Dictionary<string, object> data)
    {
        var prefs = new JsonPrefs(data);
        bool haveApplyedChanges = false;

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
            haveApplyedChanges = true;
        }

        if (data.ContainsKey("Experience"))
        {
            Experience = prefs.ValueInt("Experience", Experience);
            haveApplyedChanges = true;
        }

        if (data.ContainsKey("Fuel"))
        {
            fuel = prefs.ValueDouble("Fuel", fuel);
            Messenger.Send(EventId.FuelUpdated, new EventInfo_I((int)fuel));
            haveApplyedChanges = true;
        }

        if (data.ContainsKey("MaxFuel"))
        {
            maxFuel = prefs.ValueInt("MaxFuel", maxFuel);
            haveApplyedChanges = true;
        }

        if (data.ContainsKey("LastVisit"))
        {
            lastVisit = prefs.ValueDouble("LastVisit", lastVisit);
            if (HangarController.Instance) {
                HangarController.Instance.RefuelByTime(GameData.CurrentTimeStamp - lastVisit);
            }
            haveApplyedChanges = true;
        }

        if (data.ContainsKey("IsFuelForInviteAvailable"))
        {
            bool lastSate = isFuelForInviteAvailable;
            isFuelForInviteAvailable = prefs.ValueBool("IsFuelForInviteAvailable", true);
            if (lastSate != isFuelForInviteAvailable)
            {
                if (null != SocialSettings.OnLastInviteChanged)
                {
                    SocialSettings.OnLastInviteChanged();
                    if (!isFuelForInviteAvailable)
                    {
                        Messenger.Send(EventId.FriendInviteSuccess, null);
                    }
                }
            }
        }

        if (data.ContainsKey("CurrentTank"))
        {
            CurrentVehicle = prefs.ValueInt("CurrentTank", CurrentVehicle);
            haveApplyedChanges = true;
        }

        if (data.ContainsKey("DoubleExpTanks"))
        {
            GetDoubleExpVehiclesList(prefs.ValueObjectList("DoubleExpTanks"));
            haveApplyedChanges = true;
        }

        if (data.ContainsKey("CompletedTutorials"))
        {
            LoadTutorials(prefs.ValueObjectDict("CompletedTutorials"));
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
            xdevsOffers = new Dictionary<string, XDevsOffersStates>();
            foreach (var offerState in prefs.ValueObjectDict("XDevsOffers")) {
                try {
                    xdevsOffers[offerState.Key] = (XDevsOffersStates)Enum.Parse(typeof(XDevsOffersStates), prefs.ValueString("XDevsOffers/" + offerState.Key + "/status"), true);
                }
                catch {
                    Debug.LogError("Can't parse offer state for game " + offerState.Key);
                }
            }
            haveApplyedChanges = true;
        }

        if (prefs.Contains("Consumables"))
        {
            LoadInventory(prefs.ValueObjectDict("Consumables"));
        }

        if (prefs.Contains("ConsumableInventoryPanelItems"))
            LoadConsumableInventoryPanelItemsFromDict(prefs.ValueObjectList("ConsumableInventoryPanelItems"));

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

        if (prefs.Contains(notificationsJsonObjectName))
        {
            notificationsList = prefs.ValueObjectList(notificationsJsonObjectName);
            haveApplyedChanges = true;
        }

        if (prefs.Contains("Ads"))
        {
            adsDict = prefs.ValueObjectDict("Ads");
            haveApplyedChanges = true;
        }

        // Обновить профиль, но дергать изменения нет необходимости
        if (data.ContainsKey("NickEntered"))
        {
            nickEntered = prefs.ValueBool("NickEntered");
            haveApplyedChanges = true;
        }
        
        nickRejected = prefs.ValueBool("NickRejected", nickRejected);
        daysInARow = prefs.ValueInt("DaysInARow", daysInARow);
        ratedForBonus = prefs.ValueBool("RatedForBonus", ratedForBonus);
        isGameRated = prefs.ValueBool("IsGameRated", isGameRated);

        nextDayServerTime = prefs.ValueDouble("NextDayServerTime", nextDayServerTime);

        dailyBonusIndex = prefs.ValueInt ("DailyBonusIndex", dailyBonusIndex);
        dailyBonusIsObtained = prefs.ValueBool("DailyBonusIsObtained", dailyBonusIsObtained);
        goldKitAwardIsObtained = prefs.ValueBool("GoldKitAwardIsObtained", goldKitAwardIsObtained);
        goldKitAwardVal = prefs.ValueInt("GoldKitAwardVal", goldKitAwardVal);
        lastLevelUpAward = prefs.ValueInt("LastLevelUpAward", lastLevelUpAward);

        #region Bank Offers limits
        
        if (data.ContainsKey ("IsGoldDiscountLimitReached") || data.ContainsKey ("IsSilverDiscountLimitReached") || data.ContainsKey ("IsVipDiscountLimitReached"))
        {
            isGoldDiscountLimitReached = prefs.ValueBool ("IsGoldDiscountLimitReached", isGoldDiscountLimitReached);
            isSilverDiscountLimitReached = prefs.ValueBool ("IsSilverDiscountLimitReached", isSilverDiscountLimitReached);
            isVipDiscountLimitReached = prefs.ValueBool ("IsVipDiscountLimitReached", isVipDiscountLimitReached);
            haveApplyedChanges = true;
        }
        
        #endregion

        if (haveApplyedChanges)
        {
            NotifyAboutChanges(
                tanksChanged:   data.ContainsKey("Tanks") ||
                                data.ContainsKey("CurrentTank") ||
                                data.ContainsKey("DoubleExpTanks"));
        }

        SetGameMode(prefs);
    }

    private static void NotifyAboutChanges(bool tanksChanged)
    {
        #region TestData Чтобы показать DailyBonus
        //dailyBonusIndex = 3;
        //dailyBonusIsObtained = false;
        #endregion

        CalcLevel();

        if (HangarController.Instance && HangarController.Instance.IsInitialized)
        {
            if (SceneManager.GetActiveScene().name != GameData.HangarSceneName) {
                VipManager.IsHangarReloadRequired = true;
                XdevsSplashScreen.SetActiveWaitingIndicator(false);//Возвращаем индикатор загрузки в дефолтную позицию. Почему его возврат при покупке випа в других скриптах не срабатывает
                Loading.loadScene(GameData.HangarSceneName);
                return;
            }
            Messenger.Send(EventId.ProfileInfoLoadedFromServer, new EventInfo_SimpleEvent());
        }

        Messenger.Send(EventId.ProfileMoneyChange, new EventInfo_II(gold, silver));

        if (tanksChanged)
            Messenger.Send(EventId.ShopInfoLoadedFromServer, new EventInfo_SimpleEvent());

        if (useDebugPanelForLogging)
        {
            DT.useDebugPanelForLogging = true;
            XdevsSplashScreen.InstantiateDebugPanel ();
        }
    }

    public static int ExperienceForLevel(int _level)
    {
        if (_level < 2)
            return 0;

        return (int)((Math.Pow(2, (double)_level / 2 - 1)) * 1000) - 500;
    }

    public static int LevelForExperience(int _experience)
    {
        return (int)(2.0 * (1.0 + Math.Log((double)(_experience + 500) / 1000, 2.0)));
    }

    public static string Version
    {
        get { return version; }
        set { version = value; }
    }

    public static bool ImportantUpdate
    {
        get { return importantUpdate; }
        set { importantUpdate = value; }
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

    private static void LoadConsumableInventoryPanelItemsFromDict(List<object> list)
    {
        consumableInventoryPanelItems.Clear();
        for (int i = 0; i < ConsumablesInventory.CAPACITY; i++)
            consumableInventoryPanelItems.Add(i < list.Count ? Convert.ToInt32(list[i]) : ConsumablesInventory.EMPTY_CELL_ID);
        #region Лог приема инвентаря расходок
        //string s = "";
        //for (int i = 0; i < consumableInventoryPanelItems.Count; i++)
        //    s += string.Format("{0}slot {1} = {2}", s.Length > 0 ? ", ": "", i, consumableInventoryPanelItems[i]);
        //Debug.LogError(LoadConsumableInventoryPanelItemsFromDict.  + s);
        #endregion
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

    private static void LoadTutorials(Dictionary<string, object> dict = null)
    {
        if (dict == null) { // Got old profile.
            dict = new Dictionary<string, object> ();
        }

        foreach (var tutor in accomplishedTutorials.Keys.ToList ()) {
            if (dict.ContainsKey (tutor.ToString ())) {
                accomplishedTutorials[tutor] = Convert.ToBoolean (dict[tutor.ToString ()]);
            }
            else {
                accomplishedTutorials[tutor] = GetTutorDefaultValue (tutor);
            }
        }
    }

    private static bool GetTutorDefaultValue (Tutorials tutor) {
        switch (tutor) {
            case Tutorials.battleTutorial:
            case Tutorials.enterName:
                return Level > 1;

            case Tutorials.goToBattle:
                return Level > 2;

            case Tutorials.vehicleUpgrade:
            case Tutorials.buyCamouflage:
                return Level > 3;

            case Tutorials.buyVehicle:
                return vehicleUpgrades.Count > 1; // Tanks count > 1

            default:
                return false;
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
                Debug.Log("cant parse socialactivity: "+activity);
            }
        }
    }
    public static void LoadDefaults()
    {
        Debug.Log("Load defaults");
        enterForFirstTime = true;
        ratedForBonus = false;
        gold = START_GOLD;
        silver = START_SILVER;
        fuel = START_FUEL;
        maxFuel = START_MAX_FUEL;
        experience = 0;
        CalcLevel();
        vehicleUpgrades = new Dictionary<int, VehicleUpgrades>();
        doubleExpVehicles = new List<int>();
        CurrentVehicle = 1;
        dailyBonusIndex = 0;
        launchesCount = 0;
        nickEntered = false;
        daysInARow = 0;
        //vehicles.Add(1, new VehicleUpgrades(HangarController.Instance.defaultVehicleInfo));
        doubleExpVehicles.Add(CurrentVehicle);
        lastProfileSaveTimestamp = 0;
        VipExpirationDate = (int)MiscTools.unixOrigin;
        avatarOption = (int) AvatarOption.showEverything;
        controlOption = (int) ControlOption.joystick;
        isHideMyFlag = false;
        isSliderControl = true;
        isVoiceDisabled = false;
        LoadTutorials ();
        consumableInventoryPanelItems = ConsumablesInventory.DefaultInventoryList;
    }

    private static void ParseKits(Dictionary<string,object> kitsDict)
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
        if(prefs.Contains("GameMode"))
        {
            int tempGameMode = prefs.ValueInt("GameMode");
            GameData.Mode = Enum.IsDefined(typeof(GameData.GameMode), tempGameMode) ? (GameData.GameMode)tempGameMode : GameData.DefaultGameMode;
            //DT.LogError("ProfileInfo. Loaded GameMode = {0}", parsedValue);
        }
    }

    private static void LoadInventory(Dictionary<string, object> consumablesDict)
    {
        consumableInventory.Clear();
        foreach (Dictionary<string, object> consumableDict in consumablesDict.Values)
        {
            try
            {
                int id = 0, count = 0;
                if (!consumableDict.Extract("id", ref id) || !consumableDict.Extract("count", ref count))
                    return;

                consumableInventory.Add(id, count);
            }
            catch (Exception)
            {
                Debug.LogError("Error while parsing inventory data");
            }
        }
    }

    /// <summary>
    /// Есть ли у меня определенная расходка
    /// </summary>
    public static bool HaveConsumable(int id)
    {
        return consumableInventory != null && consumableInventory.ContainsKey(id) && consumableInventory[id] > 0;
    }
}
