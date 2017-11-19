using System.Collections.Generic;
using System.Text.RegularExpressions;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;

namespace XD
{
    public class Profile : MonoBehaviour, IProfile
    {
        [SerializeField]
        private bool                debug = false;
        [SerializeField]
        private LevelCalculator     levelCalculator = null;
        [SerializeField]
        private int                 victoriesToRate = 4;

        private int                 currentStep = 0;

        private readonly Currency   balance = new Currency();
        private ObscuredFloat       vipExpRate = 1.5f;
        private ObscuredFloat       vipSilverRate = 1.2f;
        private ObscuredBool        isCheater = false;
        
        private ObscuredInt         hangarSlots = 3;
        private int                 currentUnit = -1;
        
        private ObscuredBool        newUser = false;
        private ObscuredBool        skipLogin = false;

        private ObscuredInt         serverId = -1;
        private ObscuredInt         blocId = -1;
        private ObscuredBool        hangarTutorialCompleted = false;
        private int                 lastLevelUp = 1;
        private int                 victories = 0;
        private bool                rated = false;
        
        private Dictionary<RuntimePlatform, string> storeUrls
        {
            get
            {
                return new Dictionary<RuntimePlatform, string>{
                    { RuntimePlatform.Android, "https://play.google.com/store/apps/details?id=com.extremedevelopers.tankforce" },
                    { RuntimePlatform.IPhonePlayer, "https://itunes.apple.com/us/app/tank-force-modern-warfare/id1211576995" },
                    { RuntimePlatform.WSAPlayerARM, "https://www.microsoft.com/store/apps/9p26r81l3j3f" },
                    { RuntimePlatform.WindowsPlayer, "http://store.steampowered.com/app/604500/Tank_Force/" }
                };
            }
        }

        public bool IsModerator
        {
            get;
            private set;
        }

        public int VIPTime
        {
            get
            {
                return (int)(ProfileInfo.VipExpirationDate - GameData.CorrectedCurrentTimeStamp);
            }
        }

        public string Region
        {
            get
            {
                return StaticType.ServerList.Instance<IServerList>().ServerName;
            }
        }

        public bool BattleTutorialCompleted
        {
            get
            {
                return ProfileInfo.IsBattleTutorialCompleted;
            }
        }

        public bool HangarTutorialCompleted
        {
            get
            {
                Debug.LogFormat("Get Hangar Tutorial as '{0}'", hangarTutorialCompleted);
                return hangarTutorialCompleted;
            }

            set
            {
                Debug.LogFormat("Set Hangar Tutorial as '{0}'", value);
                hangarTutorialCompleted = value;

                if (hangarTutorialCompleted)
                {
                    ProfileInfo.TutorialIndex = (int)Tutorials.HangarTutorial + 1;
                    ProfileInfo.SaveToServer();
                }
            }
        }

        public bool Inited
        {
            get;
            set;
        }

        public int ID
        {
            get;
            private set;
        }

        public bool NewUser
        {
            get
            {
                return newUser && !skipLogin;
            }
        }

        public string PlayerName
        {
            get;
            private set;
        }

        public CurrencyValue ChangeNickPrice
        {
            get
            {
                CurrencyType type = GameData.changeNickPrice.currency == ProfileInfo.PriceCurrency.Gold ? CurrencyType.Gold : CurrencyType.Silver;
                CurrencyValue value = new CurrencyValue(type, ProfileInfo.nickRejected && !ProfileInfo.nickEntered ? 0 : (int)GameData.changeNickPrice.value);
                return value;
            }
        }

        public CurrencyValue HangarSlotPrice
        {
            get
            {
                CurrencyValue[] currencies = StaticType.MainData.Instance<IMainData>().HangarSlotPrices;
                
                int index = hangarSlots - StaticContainer.MainData.StartHangarSlots;
                //Debug.LogError("hangarSlots: " + hangarSlots + ", start: " + StaticContainer.MainData.StartHangarSlots + ", res: " + index);

                if (currencies.Length > index && index >= 0)
                {
                    return currencies[index];
                }

                return new CurrencyValue(CurrencyType.Gold, -1);
            }
        }

        public int HangarSlots
        {
            get
            {
                return hangarSlots;
            }

            set
            {
                hangarSlots = value;
            }
        }

        public int EmptySlots
        {
            get
            {
                return hangarSlots - StaticContainer.MainData.MyUnits.Count;
            }
        }

        public bool HasMaxSlots
        {
            get
            {
                return hangarSlots == 10; //КОСТЫЛЬ
            }
        }

        public bool HasFreeHangarSlot
        {
            get
            {
                return EmptySlots > 0;
            }
        }

        public Currency Balance
        {
            get
            {
                return balance;
            }
        }

        public string PlayerEmail
        {
            get;
            private set;
        }

        public bool PlayerEmailConfirmed
        {
            get;
            private set;
        }

        public bool IsVip 
        {
            get;
            private set;
        }

        public float VipExpRate
        {
            get
            {
                return vipExpRate;
            }
        }

        public float VipSilverRate
        {
            get
            {
                return vipSilverRate;
            }
        }

        public bool IsCheater
        {
            get
            {
                return isCheater;
            }
        }        

        public ILevelCalculator LevelCalculator
        {
            get
            {
                return levelCalculator;
            }
        }

        #region ISubscriber
        public string Description
        {
            get
            {
                return "[Profile] " + PlayerName;
            }

            set
            {
            }
        }

        /// <summary>
        /// Реакция на события.
        /// </summary>
        public void Reaction(Message message, params object[] parameters)
        {
            switch (message)
            {
                case Message.ServerDataReceived:
                    switch (parameters.Get<ServerDataType>())
                    {
                        case ServerDataType.ProfileData:
                            Event(message, parameters);

                            SetCurrentUnit(parameters.Get<int>(1));
                            if (blocId == -1)
                            {
                                blocId = parameters.Get<int>(2);
                            }

                            Dictionary<string, object> units = parameters.Get<Dictionary<string, object>>();
                            if (units == null)
                            {                                
                                return;
                            }

                            foreach (Dictionary<string, object> unit in units.Values)
                            {
                                int unitID = unit.ExtractOrDefault<int>("tankId");
                                IUnitHangar unitHangar = StaticContainer.MainData.GetUnitHangar(unitID);

                                if (unitHangar == null)
                                {
                                    Debug.LogError("Unit hangar '" + unitID + "'", this);
                                    continue;
                                }

                                unitHangar.Init();

                                List<object> consumables = unit.ExtractOrDefault<List<object>>("consumables");

                                unitHangar.Status = (Status)unit.ExtractOrDefault<int>("status");
                                
                                for (int j = 0; j < consumables.Count; j++)
                                {
                                    Dictionary<string, object> consumable = (Dictionary<string, object>)consumables[j];
                                    int slotId = consumable.ExtractOrDefault<int>("slotId");
                                    int consId = consumable.ExtractOrDefault<int>("id");
                                    int count = consumable.ExtractOrDefault<int>("count");
                                    if (debug)
                                    {
                                        Debug.LogWarningFormat("Try to install consumables to unit '{0}' => [{1}][{2}][{3}]", unitHangar.Name, slotId, consId, count);
                                    }
                                    unitHangar.OnInstallConsumable(slotId, consId, count, count);
                                }
                            }
                            Event(Message.BalanceChanged);
                            break;
                    }
                    break;

                case Message.WindowClosed:
                    if (parameters.Get<PSYWindow>() == PSYWindow.RankUp)
                    {
                        Event(Message.DataRequest, DataType.Server, DataKey.GetLevelUpAward);
                    }
                    break;

                case Message.PlatformUserLoginned:
                    CheckBloc();
                    break;

                case Message.AnyKeyPressed:
                    Debug.LogFormat(this, "{0}: reacton on '{1}' with parameters '{2}'".FormatString(Color.green), name, message, parameters.Length);

                    if (NewUser && !StaticType.SocialSettings.Instance<ISocialSettings>().GetSocialService().IsLoggedIn)
                    {
                        NextWindow(PSYWindow.Login, PlayerName);
                    }
                    else
                    {
                        CheckBloc();
                    }
                    break;

                case Message.ProfileInited:
                    HangarSlots = parameters.Get<int>();
                    ApplyPlayerInfo();
                    break;

                case Message.PlayerActionRequest:
                    switch (parameters.Get<PlayerActionRequest>())
                    {
                        case PlayerActionRequest.RateGame:
                            Event(Message.DataRequest, DataType.Server, DataKey.RateGameConfirm);
                            break;
                    }
                    break;

                case Message.DataRequest:
                    if (parameters.Get<DataType>() == DataType.UI)
                    {
                        switch (parameters.Get<DataKey>())
                        {
                            case DataKey.ChangeNickName:
                                string newNick = parameters.Get<string>();
                                Regex regex = new Regex(@"([\s\w-\']+(?:[\s\w-\']+)?)", RegexOptions.IgnoreCase);

                                Match match = regex.Match(newNick);
                                if (match.Length != newNick.Length)
                                {
                                    //Debug.LogError(match.Length + " / " + newNick.Length + ", group: " + match.Groups[0]);
                                    Event(Message.MessageBox, MessageBoxType.Notification, "UI_MB_IncorrectNickTitle", "UI_MB_IncorrectNickText", "UI_Ok");
                                    return;
                                }

                                Dictionary<string, string> stringParameters = new Dictionary<string, string>();
                                stringParameters.Add("nick", parameters.Get<string>());
                                Event(Message.DataRequest, DataType.Server, DataKey.ChangeNickName, stringParameters);
                                break;

                            case DataKey.SkipNickName:
                                Event(Message.DataRequest, DataType.Server, DataKey.SkipNickName);
                                break;

                            case DataKey.PickParty:
                                blocId = (int)parameters.Get<TankOwner>();
                                Dictionary<string, int> intParameters = new Dictionary<string, int>();
                                intParameters.Add("blocId", blocId);
                                Event(Message.DataRequest, DataType.Server, DataKey.PickParty, intParameters);
                                break;
                        }
                    }
                    break;

                case Message.DataResponse:
                    if (parameters.Get<DataType>() == DataType.Server)
                    {
                        switch (parameters.Get<DataKey>())
                        {
                            case DataKey.RateGameConfirm:
                                string url = "";
                                if (storeUrls.TryGetValue(Application.platform, out url))
                                {
                                    Application.OpenURL(url);
                                }
                                break;

                            case DataKey.RemoveFriend:
                                break;

                            case DataKey.ChangeNickName:
                                Debug.LogFormat(this, "{0}: reacton on '{1}' with parameters '{2}'".FormatString(Color.green), name, message, parameters.Length);
                                if (!parameters.Get<bool>())
                                {
                                    Debug.LogError("Some error on server! May be incorrect nick name?");
                                    return;
                                }
                                PlayerName = parameters.Get<Dictionary<string, string>>()["nick"];
                                CheckBloc();
                                break;

                            case DataKey.SkipNickName:
                                Debug.LogFormat(this, "{0}: reacton on '{1}' with parameters '{2}'".FormatString(Color.green), name, message, parameters.Length);

                                CheckBloc();
                                break;

                            case DataKey.GetLevelUpAward:
                                //Debug.LogErrorFormat("DataKey.GetLevelUpAward '{0}'", parameters.Get<bool>());
                                if (parameters.Get<bool>())
                                {
                                    //ApplyPlayerInfo();
                                }
                                break;

                            case DataKey.PickParty:
                                Debug.LogFormat(this, "{0}: reacton on '{1}' with parameters '{2}'".FormatString(Color.green), name, message, parameters.ToFullString());

                                if (BattleTutorialCompleted)
                                {
                                    StaticContainer.MainData.BeforeHangarEnter();
                                }
                                else
                                {
                                    Event(Message.LoadMapRequest, MapType.Tutorial);
                                }
                                break;
                        }
                    }
                    break;

                case Message.TutorialComplete:
                    hangarTutorialCompleted = true;
                    break;

                case Message.ServerPicked:
                    Debug.LogFormat(this, "{0}: reacton on '{1}' with parameters '{2}'".FormatString(Color.green), name, message, parameters.Length);
                    serverId = parameters.Get<int>();
                    PlayerPrefs.SetInt("ServerId", serverId);
                    break;

                case Message.Button:
                    switch (parameters.Get<ButtonKey>())
                    {
                        case ButtonKey.Support:
                            ClansManager.Instance.OpenAccountSupportPage();
                            //string serverAdress = StaticType.ServerList.Instance<IServerList>()
                            //Http.Manager.Instance().sessionToken
                            break;
                    }
                    break;
            }
        }
        #endregion

        #region ISender
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
                return StaticType.Profile;
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

        private void CheckDaily()
        {
            if (StaticType.MainData.Instance<IMainData>().Rewards.Obtained)
            {
                return;
            }

            //if (!windowEquipmentAppeared)
            //{
            //    Debug.LogErrorFormat("Request for daily 'false' - hangar was not loaded!");
            //    return;
            //}
            //
            //if (!profileLoaded)
            //{
            //    Debug.LogErrorFormat("Request for daily 'false' - profile was not loaded!");
            //    return;
            //}

            if (!HangarTutorialCompleted)
            {
                Debug.LogErrorFormat("Request for daily 'false' - hangar tutorial was not completed!");
                return;
            }

            if (StaticType.SceneManager.Instance<ISceneManager>().InBattle)
            {
                Debug.LogErrorFormat("Request for daily 'false' - player in battle!");
                return;
            }

            NextWindow(ARPage.DailyBonus, StaticContainer.MainData.Rewards);
            StaticType.MainData.Instance<IMainData>().Rewards.Obtained = true;
        }

        private void SetCurrentUnit(int unit)
        {
            currentUnit = unit;
            //StaticType.MainData.Instance().Reaction(Message.UnitHangarSelected, currentUnit);
        }

        public virtual void CheckBloc(bool loadTutorial = true)
        {
            if (blocId == -1)
            {
                NextWindow(PSYWindow.PickParty);
            }
            else
            {
                if (!BattleTutorialCompleted)
                {
                    Event(Message.LoadMapRequest, MapType.Tutorial);
                }
                else
                {
                    NextWindow(ARPage.Hangar);
                    CheckTutorial();
                    CheckDaily();
                    CheckRankUps();
                    //CheckNotifications();
                }
            }           
        }

        public void CheckRate()
        {
            if (!HangarTutorialCompleted)
            {
                return;
            }

            if (rated)
            {
                return;
            }

            PlayerPrefs.SetInt("VictoriesToRate", PlayerPrefs.GetInt("VictoriesToRate", 0) + 1);            

            if (PlayerPrefs.GetInt("VictoriesToRate", 0) >= victoriesToRate)
            {                
                Event(Message.LayoutRequest, PSYWindow.RateGame, new RateGame( GameData.awardForRateGame.ToCurrencyValue(), true));
                PlayerPrefs.SetInt("VictoriesToRate", 0);
            }
        }

        public void CheckTutorial()
        {
            if (!HangarTutorialCompleted)
            {
                Event(Message.TutorialRequest);
            }
        }
                          
        private void NextWindow(params object[] parameters)
        {
            Event(Message.LayoutRequest, parameters);
        }

        public void CheckRankUps()
        {
            if (!HangarTutorialCompleted)
            {
                return;
            }

            for (int level = lastLevelUp + 1; level <= LevelCalculator.Level; level++)
            {
                int silverAward = level * GameData.levelUpAwardCoefficientSilver;
                int goldAward = (int)Mathf.Ceil(level * GameData.levelUpAwardCoefficientGold);

                RankUp rankUp = new RankUp(level, silverAward, goldAward);

                Event(Message.LayoutRequest, PSYWindow.RankUp, rankUp);
            }
        }

        //public void CheckNotifications()
        //{
        //    if (!HangarTutorialCompleted)
        //    {
        //        return;
        //    }
        //
        //    //StaticType.MailBox.Instance<IMailBox>().CheckNotifications();
        //}

        //private void NextWindow(PSYWindow currentWindow)
        //{
        //    //int index = windowsOrder.IndexOf(currentWindow) + 1;
        //    //this.currentWindow = windowsOrder[index];
        //    Event(Message.LayoutRequest, currentWindow, PlayerName);
        //}


        /// <summary>
        /// Применение данных из ProfileInfo (TEMPORARY)
        /// </summary>
private void ApplyPlayerInfo()
        {
            hangarTutorialCompleted = ProfileInfo.TutorialIndex > 1;
            LevelCalculator.SetExperience(ProfileInfo.Experience);
            lastLevelUp = ProfileInfo.lastLevelUpAward;
            IsModerator = ProfileInfo.PlayerPrivilege == ProfileInfo.PlayerPrivileges.Moderator;
            rated = ProfileInfo.isGameRated;

            if (!HangarTutorialCompleted)
            {
                if (FindObjectOfType<HangarTutorial>() == null)
                {
                    Instantiate(Resources.Load("System/HangarTutorial"));
                }
            }

            //profileLoaded = true;
            //CheckDaily();
            serverId = PlayerPrefs.GetInt("ServerId", -1);

            if (PlayerName != (string)ProfileInfo.PlayerName)
            {
                PlayerName = ProfileInfo.PlayerName;
                Event(Message.NickChanged);
            }
            
            ID = ProfileInfo.playerId;
            PlayerEmail = ProfileInfo.PlayerEmail;
            PlayerEmailConfirmed = ProfileInfo.PlayerEmailConfirmed;
            IsVip = ProfileInfo.IsPlayerVip;
            isCheater = ProfileInfo.IsCheater;
            
            balance.Gold = ProfileInfo.Gold;
            balance.Silver = ProfileInfo.Silver;
            balance.Expa = ProfileInfo.ExperiencePoints;
            newUser = !ProfileInfo.nickEntered;
            skipLogin = ProfileInfo.nickRejected;

            Inited = true;
        }

        public void ChangeBalance(CurrencyValue value, bool local = false)
        {
            //Debug.LogError("ChangeBalance: " + value + ", local: " + local);
            Event(Message.BalanceChanged);
            /*if (local)
            {
                CurrencyValue val = balance[value.Type];
                val.SetAmount(val.Amount + value.Amount);
            }*/
        }

        public void AddHangarSlot()
        {
            HangarSlots++;
        }

        private void Awake()
        {
            DontDestroyOnLoad(this);
            SaveInstance();
        }

        private void Start()
        {
            StaticType.UI.AddSubscriber(this); 
            StaticType.StaticContainer.AddSubscriber(this);
            StaticType.DataHandler.AddSubscriber(this);
            StaticType.SceneManager.AddSubscriber(this);
            StaticType.DataContainer.AddSubscriber(this);
            StaticType.SocialSettings.AddSubscriber(this);

            AddSubscriber(StaticType.UI.Instance());
            AddSubscriber(StaticType.DataHandler.Instance());
        }

        private void OnDestroy()
        {
            StaticType.StaticContainer.RemoveSubscriber(this);
            StaticType.DataHandler.RemoveSubscriber(this);
            StaticType.UI.RemoveSubscriber(this);
            StaticType.SceneManager.RemoveSubscriber(this);
            StaticType.DataContainer.RemoveSubscriber(this);
            StaticType.SocialSettings.RemoveSubscriber(this);

            DeleteInstance();
        }
    }
}