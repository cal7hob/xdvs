using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tanks.Models;
using UnityEngine;
using Facebook.Unity;


namespace SocialNetworks
{
    public class Facebook : MonoBehaviour, ISocialService
    {
#pragma warning disable 67
        public event Action<ISocialService, SocialInitializationState> InitializationFinished = delegate { };
        public event Action<ISocialService> LoginSucceed = delegate { };
        public event Action LoginFail = delegate { };
        public event Action LogoutSucceed = delegate { };
        public event Action<string> GroupJoined = delegate { };
        public event Action GroupJoinErrorOccured = delegate { };
#pragma warning restore 67

        private bool isAppPageOpened;

        private List<string> requiredPermissions = new List<string>()
        {
            "public_profile",
            "email",
            "user_friends"
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
        public Player Player { get; private set; }

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

        public string AvatarUrl()
        {
            return string.IsNullOrEmpty(Uid()) ? "" : "https://graph.facebook.com/" + Uid() + "/picture?type=square";
        }

        public void InviteFriend()
        {
            Screen.SetResolution(960, 600, false);
            FacebookManager.FriendsInviteRequest();
        }

        public Dictionary<string, string> GetAuthParams()
        {
            return new Dictionary<string, string>() {
                {"facebook", FacebookManager.UserId},
                {"fbApp", FB.AppId}
            };
        }


        private Dictionary<string, SocialPrice> prices = new Dictionary<string, SocialPrice>();
        //{
        //    {"xdevs.50_000_silver", new SocialPrice(1.99f, SocialCurrency.Dollar) },
        //    {"xdevs.150_000_silver", new SocialPrice(4.99f, SocialCurrency.Dollar) },
        //    {"xdevs.400_000_silver", new SocialPrice(9.99f, SocialCurrency.Dollar) },
        //    {"xdevs.1_250_000_silver", new SocialPrice(24.99f, SocialCurrency.Dollar) },
        //    {"xdevs.3_000_000_silver", new SocialPrice(49.99f, SocialCurrency.Dollar) },
        //    {"xdevs.7_000_000_silver", new SocialPrice(99.99f, SocialCurrency.Dollar) },
        //    {"xdevs.44_gold", new SocialPrice(1.99f, SocialCurrency.Dollar) },
        //    {"xdevs.115_gold", new SocialPrice(4.99f, SocialCurrency.Dollar) },
        //    {"xdevs.250_gold", new SocialPrice(9.99f, SocialCurrency.Dollar) },
        //    {"xdevs.750_gold", new SocialPrice(24.99f, SocialCurrency.Dollar) },
        //    {"xdevs.1_650_gold", new SocialPrice(49.99f, SocialCurrency.Dollar) },
        //    {"xdevs.3_400_gold", new SocialPrice(99.99f, SocialCurrency.Dollar) },
        //    {"xdevs.1_vip", new SocialPrice(0.99f, SocialCurrency.Dollar) },
        //    {"xdevs.3_vip", new SocialPrice(2.99f, SocialCurrency.Dollar) },
        //    {"xdevs.7_vip", new SocialPrice(5.99f, SocialCurrency.Dollar) },
        //    {"xdevs.30_vip", new SocialPrice(17.99f, SocialCurrency.Dollar) },
        //    {"xdevs.vip_kit", new SocialPrice(4.99f, SocialCurrency.Dollar) },
        //    {"xdevs.gold_kit", new SocialPrice(2.99f, SocialCurrency.Dollar) },
        //    {"xdevs.vehicle_kit", new SocialPrice(0.99f, SocialCurrency.Dollar) }
        //};

        public SocialPrice GetPriceById(string item_id)
        {
            if (prices.Count == 0)
            {
                foreach (var socialPrice in GameData.socialPrices)
                {
                    float price = Convert.ToSingle(socialPrice.Value, System.Globalization.CultureInfo.InvariantCulture);
                    prices.Add(socialPrice.Key, new SocialPrice(price, SocialCurrency.Dollar));
                }
            }
            return prices[item_id];

        }

        public string GetPriceStringById(string item_id)
        {
            return prices[item_id].PriceString;
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
            GroupJoined((string)GameData.socialGroups["fb"]);
            bool joined = ProfileInfo.socialActivity.Contains(SocialAction.joined);
            if(!joined)SocialSettings.Instance.ReportSocialActivity(SocialAction.joined);
#endif
        }

        public void JoinGroup(string id)
        {
            isAppPageOpened = true;
            Application.OpenURL("https://www.facebook.com/" + id);
        }

        public IEnumerable<SocialNetworkGroup> AllSocialGroups { get; private set; }

        void Start()
        {
            AllSocialGroups = new List<SocialNetworkGroup>();
            SocialNetworkInfo = new SocialNetworkInfo(SocialPlatform.Facebook, "Facebook", "linkPanelfb");
#if UNITY_WEBGL
            Application.ExternalEval("try{OnLoaded();}catch(e){}");
#endif
            if (!FB.IsInitialized)
            {
                FB.Init(OnInitComplete);
            }
            else
            {
                OnInitComplete();
            }
        }

        void OnInitComplete()
        {
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
                    LoginCallback(new LoginResult(new ResultContainer("")));
                else
                {
                    Login();
                }
            }
#endif
        }

        private void LoginCallback(ILoginResult result)
        {
            if (FB.IsLoggedIn)
            {
                FB.API("/me?fields=first_name,last_name", HttpMethod.GET, graphResult =>
                {
                    if (string.IsNullOrEmpty(graphResult.Error))
                    {
                        Player = new Player(SocialPlatform.Facebook,
                            Uid(),
                            (string)graphResult.ResultDictionary["first_name"],
                            (string)graphResult.ResultDictionary["last_name"],
                            string.Format("https://graph.facebook.com/{0}/picture?type=square", Uid()));
#if !(UNITY_WEBGL || UNITY_WEBPLAYER)
                        if (GameData.IsHangarScene)
                        {
                            CheckAppPage(b => LoginSucceed(this));
                        }
                        else
                        {
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
            CheckAppPage();
        }

        private void CheckAppPage(Action<bool> onComplete = null)
        {
            var appPageId = (string)GameData.socialGroups["fb"];
            FB.API("/me/likes/" + appPageId, HttpMethod.GET, delegate (IGraphResult likes)
            {
                //Debug.Log("/me/likes "+ likes.RawResult);
                var data = new JsonPrefs(likes.ResultDictionary).ValueObjectList("data");
                var liked = data.Count > 0;
                var info = new SocialNetworkGroupInfo(appPageId, Application.productName + " App Page", "Facebook",//todo App page localization?
                                          "https://www.facebook.com/" + appPageId);
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

            public PaymentResult(IPayResult result, string _itemId)
            {
                data = result.RawResult;
                itemId = _itemId;
                if (!string.IsNullOrEmpty(result.Error))
                {
                    Debug.LogError("Facebook payment response error: " + result.Error);
                    return;
                }

                var d = result.ResultDictionary;
                if (d == null)
                {
                    Debug.LogError("Facebook payment response error: JSON deserialize result is null");
                    return;
                }

                if (d.ContainsKey("payment_id") && d.ContainsKey("signed_request"))
                {
                    code = 0;
                }
            }

            public bool isOk()
            {
                return code == 0;
            }
        }

        public void ShowPayment(string itemId)
        {
#if UNITY_WEBPLAYER || UNITY_WEBGL
            //var req = Http.Manager.Instance ().CreateRequest ("/billing/fb/initiate");
            FB.Canvas.Pay(
                product: Http.Manager.CurrentServer + "/billing/fb/item/" + itemId,
                quantity: 1,
                callback: delegate (IPayResult result)
                {
                    try
                    {
                        XdevsSplashScreen.SetActiveWaitingIndicator(false);
                        if (!string.IsNullOrEmpty(result.Error))
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
                        Http.Manager.ReportException("Billing", e);
                    }
                }
            );
#endif
        }

        private void PaymentCallback(PaymentResult result)
        {
            var req = Http.Manager.Instance().CreateRequest("/billing/fb/check");
            req.Form.AddField("data", result.data);
            req.Form.AddField("item", result.itemId);
            Http.Manager.StartAsyncRequest(req, (res) =>
            {
                ProfileInfo.SaveToServer();
            });
        }

        #endregion

    }
}
