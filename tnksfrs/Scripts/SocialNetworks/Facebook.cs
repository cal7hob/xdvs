using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tanks.Models;
using UnityEngine;
using Facebook.Unity;
using XD;
using System.Collections;

namespace SocialNetworks
{
    public class Facebook : MonoBehaviour, ISocialService, ISubscriber
    {
        public event Action<ISocialService, SocialInitializationState> InitializationFinished = delegate { };
        public event Action<ISocialService> LoginSucceed = delegate { };
        public event Action LoginFail = delegate { };
        public event Action LogoutSucceed = delegate { };
        public event Action<string> GroupJoined = delegate { };
        public event Action GroupJoinErrorOccured = delegate { };

        private bool isAppPageOpened;

        private List<string> requiredPermissions = new List<string>()
        {
            "public_profile",
            "email",
            "user_friends",
            //"user_photos",
        };
        public bool IsLoggedIn
        {
            get { return FB.IsLoggedIn; }
        }

        public bool IsInitialized
        {
            get { return FB.IsInitialized; }
        }

        public SocialNetworkInfo SocialNetworkInfo { get; private set; }
        public Tanks.Models.Player Player { get; private set; }

        public void Login()
        {
            FB.LogInWithReadPermissions(requiredPermissions, LoginCallback);
        }

        public void Logout()
        {
            FB.LogOut();
            LogoutSucceed();
        }

        public string Uid()
		{
			return FacebookManager.UserId;
		}

        public string AvatarUrl ()
        {
            return string.IsNullOrEmpty(Uid()) ? "" : "https://graph.facebook.com/" + Uid() + "/picture?type=square";
        }

        public void InviteFriend ()
        {
            FacebookManager.FriendsInviteRequest ();
        }

        public Dictionary<string, string> GetAuthParams ()
        {
            return new Dictionary<string, string> () {
                {"facebook", FacebookManager.UserId},
                {"fbApp", FB.AppId}
            };
        }

        public SocialPrice GetPriceById (string item_id)
        {
            return null;
        }

        public string GetPriceStringById (string item_id)
        {
            string result = "";
            XD.StaticContainer.Localization.TryLocalize(item_id, out result);
            return result;
        }

		public void PostNewLevelToWall()
	    {
		    // РЕАЛИЗУЙ ТУТ!
	    }

        public void Post(string text, Texture2D img)
        {
            FacebookManager.Post(text, img);
        }

        public void PostAchievement(AchievementsIds.Id webAchievement, string text)
        {
        }

        public List<string> GetFriendsAppUsersIds()
        {
            return FacebookManager.Instance.GetFriendsAppUsersIds();
        }

        public void UpdateSocialActivity()
        {
#if !(UNITY_WEBGL || UNITY_WEBPLAYER)
            Debug.LogError("UpdateSocialActivity ");
            GroupJoined((string)GameData.socialGroups["fb"]);
            bool joined = ProfileInfo.socialActivity.Contains(SocialAction.joined);
            Debug.LogError("UpdateSocialActivity joined: "+joined);
            if(!joined) StaticType.SocialSettings.Instance<ISocialSettings>().ReportSocialActivity(SocialAction.joined);
#endif
        }

        public void JoinGroup(string id)
        {
            isAppPageOpened = true;
            Application.OpenURL("https://www.facebook.com/" + id);
        }
        
        public IEnumerable<SocialNetworkGroup> AllSocialGroups { get; private set; } 

        private void Start ()
        {
            #if DEBUG_TAG_TF
            Debug.Log("TF: Facebook - Start");
            #endif
            AllSocialGroups = new List<SocialNetworkGroup>();
            SocialNetworkInfo = new SocialNetworkInfo(SocialPlatform.Facebook, "Facebook", "linkPanelfb");
#if UNITY_WEBGL
            Application.ExternalEval("try{OnLoaded();}catch(e){}");
#endif
            if (!FB.IsInitialized)
            {
                Debug.Log("TF: Facebook - Init Start");
                FB.Init(OnInitComplete);
            }
            else
            {
                OnInitComplete ();
            }
            StaticType.UI.AddSubscriber(this);
            AddSubscriber(StaticType.UI.Instance());
        }

        private void OnDestroy()
        {
            RemoveSubscriber(StaticType.UI.Instance());
            StaticType.UI.RemoveSubscriber(this);
        }

        void OnInitComplete ()
        {
#if DEBUG_TAG_TF
            Debug.Log("TF: Facebook - Comlete Init");
#endif
#if !UNITY_EDITOR && (UNITY_WEBPLAYER || UNITY_WEBGL)
            if (!FB.IsLoggedIn)
            {
                FB.LogInWithReadPermissions(new List<string>() { "public_profile", "email", "user_friends" }, LoginCallback);
            }
            else
            {
                Dispatcher.Send(EventId.AfterWebPlatformDefined, null);
            }
#else
            InitializationFinished(this, SocialInitializationState.Success);
            if (FB.IsLoggedIn)
            {
                var currentPermissions = AccessToken.CurrentAccessToken.Permissions;
                var permissions = currentPermissions as IList<string> ?? currentPermissions.ToList();
                if (requiredPermissions.Intersect(permissions).Count() == requiredPermissions.Count())
                {
#if DEBUG_TAG_TF
                    Debug.Log("TF: Facebook - LoginCallback(new LoginResult(new ResultContainer()));");
#endif
                    LoginCallback(new LoginResult(new ResultContainer("")));
                }
                else
                {
#if DEBUG_TAG_TF
                    Debug.Log("TF: Facebook - Login Start");
#endif
                    Login();
                }
            }
#endif
                }

        private void LoginCallback(ILoginResult result)
        {
#if DEBUG_TAG_TF
            Debug.Log("TF: Facebook - Login Callback");
#endif
            if (FB.IsLoggedIn)
            {
#if DEBUG_TAG_TF
                Debug.Log("TF: Facebook - IsLoggined");
#endif
                FB.API("/me?fields=first_name,last_name", HttpMethod.GET, graphResult =>
                {
                    if (string.IsNullOrEmpty(graphResult.Error))
                    {
                        Player = new Tanks.Models.Player(SocialPlatform.Facebook, 
                            Uid(), 
                            (string)graphResult.ResultDictionary["first_name"], 
                            (string)graphResult.ResultDictionary["last_name"],
                            string.Format("https://graph.facebook.com/{0}/picture?type=square", Uid()));
#if !(UNITY_WEBGL || UNITY_WEBPLAYER)
                        if (!XD.StaticContainer.SceneManager.InBattle)
                        {
#if DEBUG_TAG_TF
                            Debug.Log("TF: Facebook if (!XD.StaticContainer.SceneManager.InBattle)");
#endif
                            //Отключил 2 августа 2017 для проверки. т.к. не работало из-за того что данные с сервака ещё не были получены нужные для этого действия
                            //CheckAppPage(b => LoginSucceed(this));
                            LoginSucceed(this);
                        }
                        else
                        {
#if DEBUG_TAG_TF
                            Debug.Log("TF: Facebook if (XD.StaticContainer.SceneManager.InBattle)");
#endif
                            Dispatcher.Subscribe(EventId.ServerDataReceived, CheckAppPage);
                            LoginSucceed(this);
                        }
#else
                            LoginSucceed(this);
#endif
                    }
                });
            }
            else
            {
				LoginFail();
                Debug.LogWarning("Not logged in Facebook!!!");
            }
        }

        private void CheckAppPage(EventId arg1, EventInfo arg2)
        {
#if DEBUG_TAG_TF
            Debug.LogError("TF: CheckAppPage");
#endif
            CheckAppPage();
        }

        private void CheckAppPage(Action<bool> onComplete = null)
        {
            var appPageId = (string)GameData.socialGroups["fb"];
            FB.API("/me/likes/"+appPageId, HttpMethod.GET, delegate(IGraphResult likes)
            {
                //Debug.Log("/me/likes "+ likes.RawResult);
                var data = new JsonPrefs(likes.ResultDictionary).ValueObjectList("data");
                var liked = data.Count > 0;
                var info = new SocialNetworkGroupInfo(appPageId, Application.productName + " App Page", "Facebook",//todo App page localization?
                                          "https://www.facebook.com/"+appPageId); 
                var group = new SocialNetworkGroup(info, liked, this);
                AllSocialGroups = new List<SocialNetworkGroup> { group };
                if (onComplete != null)
                {
                    onComplete(liked);
                }
            });
        }

        public void OnApplicationFocus(bool paused)
        {
            if (!paused && isAppPageOpened)
            {
                isAppPageOpened = false;
                UpdateSocialActivity();
            }
        }

#region FacebookCanvasPayments

        private class PaymentResult
        {
            private int code = -1;
            public string requestId = "";
            public string data;
            public string itemId;

            public PaymentResult (IPayResult result, string _itemId)
            {
                data = result.RawResult;
                itemId = _itemId;
                if (!string.IsNullOrEmpty(result.Error)) {
                    Debug.LogError("Facebook payment response error: " + result.Error);
                    return;
                }

                var d = result.ResultDictionary;
                if (d == null) {
                    Debug.LogError ("Facebook payment response error: JSON deserialize result is null");
                    return;
                }

                if (d.ContainsKey ("payment_id") && d.ContainsKey ("signed_request")) {
                    code = 0;
                }
            }

            public bool isOk ()
            {
                return code == 0;
            }
        }

        public void ShowPayment (string itemId)
        {
#if UNITY_WEBPLAYER || UNITY_WEBGL
            //var req = Http.Manager.Instance ().CreateRequest ("/billing/fb/initiate");
            FB.Canvas.Pay (
                product: Http.Manager.CurrentServer + "/billing/fb/item/" + itemId,
                quantity: 1,
                callback: delegate (IPayResult result){
                            try
                            {
                                //XdevsSplashScreen.SetActiveWaitingIndicator(false);
                                Event(XD.Message.LockUI, false);
                                if(!string.IsNullOrEmpty(result.Error))
                                {
                                    Debug.LogError("Facebook payment dialog error: " + result.Error);
                                    return;
                                }
                                var r = new PaymentResult(result, itemId);
                                if (r.isOk())
                                {
                                    PaymentCallback(r);
                                }
                                else
                                {
                                    Debug.LogError("Facebook payment error: " + result.RawResult);
                                }
                             }
                             catch (Exception e)
                             {
                                Http.Manager.ReportException ("Billing", e);
                             }
                          }
               );
#endif
        }

        private void PaymentCallback (PaymentResult result)
        {
            var req = Http.Manager.Instance ().CreateRequest ("/billing/fb/check");
            req.Form.AddField ("data", result.data);
            req.Form.AddField ("item", result.itemId);
            Http.Manager.StartAsyncRequest (req, (res) => {
                ProfileInfo.SaveToServer ();
            });
        }

#endregion

#region ISubscriber

        public string Description
        {
            get
            {
                return "[PlatformFactory] " + name;
            }

            set
            {
                name = value;
            }
        }

        public void Reaction(XD.Message message, params object[] parameters)
        {
            TypeExternalPlatform typePlatform = TypeExternalPlatform.Undefined;

            switch (message)
            {
                case XD.Message.PlatformLogin:
                    typePlatform = (TypeExternalPlatform)parameters.Get<TypeExternalPlatform>();
                    if(typePlatform != TypeExternalPlatform.Facebook)
                    {
                        return;
                    }
#if DEBUG_TAG_TF
                    Debug.Log("TF: Platform Login " + typePlatform);
#endif
                    Login();
                    break;
                case XD.Message.PlatformScreenshotPost:
                    ISocialService service = StaticType.SocialSettings.Instance<ISocialSettings>().GetSocialService();
                    if (service != null && service.SocialNetworkInfo != null && service.SocialNetworkInfo.Platform == SocialPlatform.Facebook)
                    {
                        string text = (string)parameters.Get<string>();
                        ScreenShot("Пробное описание к скриншоту");
                    }
                    break;
                case XD.Message.Button:
                    ButtonKey buttonKey = (ButtonKey)parameters.Get<ButtonKey>();
                    if (buttonKey != ButtonKey.SocialLogin)
                    {
                        return;
                    }
                    SocialPlatform socialPlatform = (SocialPlatform)parameters.Get<SocialPlatform>();
                    if (socialPlatform != null && socialPlatform == SocialPlatform.Facebook)
                    {
                        bool set = true;
                        set = (bool)parameters.Get<bool>();
#if DEBUG_TAG_TF
                        Debug.Log("TF: Facebook: ButtonKey.SocialLogin: " + set);
#endif
                        if (set == true)
                        {
                            //Логинимся
#if DEBUG_TAG_TF
                            Debug.Log("TF: Platform Login " + typePlatform);
#endif
                            Login();
                        }
                        else
                        {
                            //Разлогиниваемся
#if DEBUG_TAG_TF
                            Debug.Log("TF: Platform Logout " + typePlatform);
#endif
                            Logout();
                        }
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

        public void Event(XD.Message message, params object[] _parameters)
        {
            for (int i = 0; i < Subscribers.Count; i++)
            {
                Subscribers[i].Reaction(message, _parameters);
            }
        }
#endregion

        private void ScreenShot(string description)
        {
            StartCoroutine(ShareScreenshot(description));
        }

        private IEnumerator ShareScreenshot(string description)
        {
            yield return new WaitForEndOfFrame();
            TypeExternalPlatform typePlatform = TypeExternalPlatform.Facebook;
            Texture2D textureScreenshoot = MiscTools.GetScreenshot();
            Debug.Log("TF: PlatformScreenshotPost: " + typePlatform);
            Post(description, textureScreenshoot);
        }

    }
}
