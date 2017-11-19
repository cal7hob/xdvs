using System;
using Http;
using Tanks.Models;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MiniJSON;
using System.Linq;
using XD;

namespace SocialNetworks
{
    [RequireComponent(typeof (CallbackPool))]
    public class Odnoklassniki : OdnoklassnikiWebplayerAPI, ISocialService
    {
        public string UserId
        {
            get { return app_params.ContainsKey("logged_user_id") ? app_params["logged_user_id"] as string : ""; }
        }

        public string SessionKey
        {
            get { return app_params.ContainsKey("session_key") ? app_params["session_key"] as string : ""; }
        }

        public string AuthSig
        {
            get { return app_params.ContainsKey("auth_sig") ? app_params["auth_sig"] as string : ""; }
        }

        public event Action<ISocialService, SocialInitializationState> InitializationFinished = delegate { };
        public event Action<ISocialService> LoginSucceed = delegate { };
        public event Action LoginFail = delegate { };
        public event Action LogoutSucceed = delegate { };
        public event Action<string> GroupJoined = delegate { };
        public event Action GroupJoinErrorOccured = delegate { };
        public bool IsLoggedIn { get; private set; }
        public bool IsInitialized { get; private set; }
        public SocialNetworkInfo SocialNetworkInfo
        {
            get
            {
                return new SocialNetworkInfo(SocialPlatform.Odnoklassniki, "Одноклассники", "linkPanelok");
            }
        }
        public Tanks.Models.Player Player { get; private set; }

        public void Login()
        {

        }

        public void Logout()
        {
        }

        public string Uid()
        {
            return UserId;
        }

        public void InviteFriend()
        {
            if (Screen.fullScreen)
                Screen.SetResolution(960, 600, false);
            Application.ExternalCall("inviteFriend", gameObject.name, "FriendInviteCallback",
                Localizer.GetText("textVkInvitation"));
        }

        public void PostNewLevelToWall()
        {
            if (Screen.fullScreen)
                Screen.SetResolution(960, 600, false);
            Application.ExternalCall("OKAPIWrapper.new_level", ProfileInfo.Level);
        }

        public void PostAchievement(AchievementsIds.Id webAchievement, string text)
        {
            if (Screen.fullScreen)
                Screen.SetResolution(960, 600, false);
            Application.ExternalCall("OKAPIWrapper.achievement_report", webAchievement,
                text + " http://ok.ru/game/" + GameData.CurrentGame.ToString());
        }

        public void Post(string text, Texture2D img)
        {
            CheckPermission();
            StartCoroutine(UploadImage(img, delegate(string token)
            {
                if (Screen.fullScreen)
                    Screen.SetResolution(960, 600, false);
                Application.ExternalCall("OKAPIWrapper.post",
                    text + " http://ok.ru/game/" + GameData.CurrentGame.ToString(), token);
            }, LogToConsole));
        }

        private void CheckPermission()
        {
            var parameters = new Dictionary<string, string>
            {
                {"method", "users.hasAppPermission"},
                {"ext_perm", "PHOTO_CONTENT"}
            };
            callByCallbackAndParams("FAPI.Client.call",
                Json.Serialize(parameters),
                delegate(object obj, Callback callback)
                {
                    bool have_permission = new JsonPrefs((string) obj).ValueBool("data");
                    if (!have_permission)
                    {
                        Application.ExternalCall("FAPI.UI.showPermissions", "[\"PHOTO_CONTENT\"]");
                    }

                });
        }

        private IEnumerator UploadImage(Texture2D img, Action<string> OnSuccess, Action<string> OnFail)
        {
            XdevsSplashScreen.SetActiveWaitingIndicator(true);
            var parameters = new Dictionary<string, string>
            {
                {"method", "photosV2.getUploadUrl"},
                {"aid", "other"}
            };
            bool finished = false;
            bool success = false;
            string uploadUrl = string.Empty;
            string token = string.Empty;
            callByCallbackAndParams("FAPI.Client.call",
                Json.Serialize(parameters),
                delegate(object obj, Callback callback)
                {
                    string json = (string) obj;
                    var prefs = new JsonPrefs(json.Replace("\"{", "{").Replace("}\"", "}"));
                    uploadUrl = prefs.ValueString("data/upload_url");
                    finished = true;
                });
            while (!finished)
                yield return new WaitForSeconds(0.1f);
            if (!string.IsNullOrEmpty(uploadUrl))
            {
                var request = Http.Manager.Instance().CreateRequest("/upload/ok/wall");
                request.Form.AddField("uploadUrl", uploadUrl);
                request.Form.AddField("image", Convert.ToBase64String(img.EncodeToJPG()));
                finished = false;
                Http.Manager.StartAsyncRequest(request, response => finished = true);
                while (!finished)
                    yield return null;
                if (string.IsNullOrEmpty(request.GetResponse().error))
                {
                    var dict = new JsonPrefs(request.GetResponse().text).ValueObjectDict("photos");
                    foreach (var o in dict)
                    {
                        token = new JsonPrefs(o.Value).ValueString("token");
                        success = !string.IsNullOrEmpty(token);
                    }
                }
            }
            //XdevsSplashScreen.SetActiveWaitingIndicator(false);
            Event(XD.Message.LockUI, false);
            if (success)
            {
                OnSuccess(token);
            }
            else
            {
                OnFail("Error uploading photo");
            }
        }

        public string AvatarUrl()
        {
            return profile_info.ContainsKey("pic_1") ? profile_info["pic_1"] as string : "";
        }

        public void ShowPayment(string item_id)
        {
            var filteredItem = items.Single(i => i.id == item_id);
            JSshowPayment(filteredItem.name, "", filteredItem.id, filteredItem.price.Value, null, null, "ok", "true");
            //XdevsSplashScreen.SetActiveWaitingIndicator(false);
            Event(XD.Message.LockUI, false);
        }

        public SocialPrice GetPriceById(string item_id)
        {
            var filteredItem = items.Single(i => i.id == item_id);
            return filteredItem.price;
        }

        public string GetPriceStringById(string item_id)
        {
            return GetPriceById(item_id).PriceString;
        }

        public Dictionary<string, string> GetAuthParams()
        {
            return new Dictionary<string, string>()
            {
                {"odnoklassniki", UserId},
                {"session_key", SessionKey},
                {"signature", AuthSig}
            };
        }

        public List<string> GetFriendsAppUsersIds()
        {
            List<string> friends_uids = new List<string>();
            foreach (Dictionary<string, object> friend in friends)
            {
                friends_uids.Add((string) friend["uid"]);
            }
            return friends_uids;
        }

        public void UpdateSocialActivity()
        {
            StartCoroutine(updateSocialActivity());
        }

        public void JoinGroup(string id)
        {
        }

        public IEnumerable<SocialNetworkGroup> AllSocialGroups
        {
            get { return new List<SocialNetworkGroup>(); }
        }

        private List<SocialGoods> items = new List<SocialGoods>();
        private CallbackPool callbackPool;
        private Dictionary<string, object> app_params = new Dictionary<string, object>();
        private Dictionary<string, object> profile_info = new Dictionary<string, object>();
        private List<object> friends = new List<object>();
        private bool app_parameters_loaded;
        private bool profile_loaded;
        private bool friends_loaded;

        private void GetAppParameters()
        {
            Application.ExternalCall("OKAPIWrapper.unity_get_app_params", gameObject.name, "GetAppParametersCallback");
        }

        private void GetAppParametersCallback(string parameters)
        {
            LogToConsole("App parameters string: " + parameters);
            app_params = Json.Deserialize(parameters) as Dictionary<string, object>;
            LogToConsole("App parameters dictionary: ");
            foreach (var kv in app_params)
            {
                LogToConsole(kv.Key + ": " + kv.Value as String);
            }
            app_parameters_loaded = true;
        }

        public void GetUserInfo(List<string> uids, List<string> fields, Action<List<object>, bool> finishCallback = null)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>()
            {
                {"method", "users.getInfo"},
                {"uids", string.Join(",", uids.ToArray())},
                {"fields", string.Join(",", fields.ToArray())}
            };
            callByCallbackAndParams("FAPI.Client.call", Json.Serialize(parameters),
                delegate(object obj, Callback callback)
                {
                    LogToConsole("callByCallbackAndParams " + obj);
                    Dictionary<string, object> result = Json.Deserialize((string) obj) as Dictionary<string, object>;
                    if ((string) result["status"] == "ok")
                    {
                        var data = result["data"] as List<object>;
                        foreach (var v in data)
                        {
                            LogToConsole(v.ToString());
                        }
                        if (null != finishCallback)
                        {
                            finishCallback(data, true);
                        }
                    }
                    else
                    {
                        LogToConsole("GetUserInfo error: " + (string) result["error"]);
                    }
                });
        }

        private void GetProfileInfo()
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>()
            {
                {"method", "users.getCurrentUser"}
            };
            base.CallApiMethod(parameters, gameObject.name, "GetProfileInfoCallback");
        }

        protected void GetProfileInfoCallback(string param)
        {
            LogToConsole(param);
            profile_info = Json.Deserialize(param) as Dictionary<string, object>;
            profile_loaded = true;
        }

        public void GetFriendsAppUsers()
        {
#if UNITY_WEBPLAYER || UNITY_WEBGL
            Dictionary<string, string> parameters = new Dictionary<string, string>()
            {
                {"method", "friends.getAppUsers"}
            };
            //запрос друзей
            callByCallbackAndParams("FAPI.Client.call", Json.Serialize(parameters),
                delegate(object obj, Callback callback)
                {
                    LogToConsole("callByCallbackAndParams " + obj);
                    var result = Json.Deserialize((string) obj) as Dictionary<string, object>;
                    if (result != null && (string) result["status"] == "ok")
                    {
                        var data = result["data"] as Dictionary<string, object>;
                        var friends_uids = new List<string>();
                        if (data != null)
                            friends_uids = ((List<object>) data["uids"]).ConvertAll(obj1 => obj1.ToString());
                        friends_uids.Add(UserId);
                        //запрос информации о друзьях
                        GetUserInfo(friends_uids, new List<string>() {"pic_1", "first_name"},
                            delegate(List<object> objects, bool status)
                            {
                                foreach (Dictionary<string, object> friend in objects)
                                {
                                    friends.Add(friend);
                                    /*var prefs = new JsonPrefs(friend);
                        var uid = prefs.ValueString("uid", "");
                        var first_name = prefs.ValueString("first_name", "");
                        var pic = prefs.ValueString("pic_1", "");
                        if(!string.IsNullOrEmpty(uid))
                            _friends[uid] = new SocialUser(uid,pic,first_name);*/
                                }
                                friends_loaded = true;
                            });
                    }
                    else
                    {
                        //{"status":"error","data":null,"error":{"error_code":102,"error_data":null,"error_msg":"PARAM_SESSION_EXPIRED : Session expired"}}
                        LogToConsole("GetFriendsAppUsers error: " + obj);
                    }
                });
#else
	    friends_loaded = false;
#endif
        }

        protected override void APIMethodCallback(string param)
        {
            //Dictionary<string,object> obj = Json.Deserialize(param) as Dictionary<string,object>;
            base.LogToConsole("OKAPIExample APICALLBACK: " + param);
        }

        protected override void JSMethodCallback(string param)
        {
            LogToConsole(param);
            Dictionary<string, object> dict = Json.Deserialize(param) as Dictionary<string, object>;
            string method = (string) dict["method"];
            string result = (string) dict["result"];
            //string data = (string)dict["data"];
            switch (method)
            {
                case "showPermissions":
                    break;
                case "showPayment":
                    LogToConsole("Payment response received");
                    if (result == "ok")
                    {
                        ProfileInfo.SaveToServer();
                    }
                    break;
            }
        }

        private void FriendInviteCallback(string parameters)
        {
            //{"result":"cancel","data":"null"}
            Dictionary<string, object> dict = Json.Deserialize(parameters) as Dictionary<string, object>;
            LogToConsole(parameters);
            string result = (string) dict["result"];
            if ((string.IsNullOrEmpty(result)) || (result == "cancel"))
            {
                LogToConsole("Error while friends invite");
                return;
            }
            if (SocialSettings.IsBonusForInviteAvailable)
            {
                Http.Manager.FuelForInvite((fResult, response) =>
                {
                    if (fResult)
                    {
                        LogToConsole("Fuel inreased by " + GameData.fuelForInvite);

                        #region Google Analytics: fuel got via social invitation

                        GoogleAnalyticsWrapper.LogEvent(
                            new CustomEventHitBuilder()
                                .SetParameter(GAEvent.Category.FuelBuying)
                                .SetParameter(GAEvent.Action.GotViaMoimirInvitation)
                                .SetParameter<GAEvent.Label>()
                                .SetSubject(GAEvent.Subject.PlayerLevel, ProfileInfo.Level)
                                .SetValue(ProfileInfo.Gold));

                        #endregion
                    }
                });
            }
        }

        private void Awake()
        {
            callbackPool = CallbackPool.instance;
            callbackPool.initialize();
            StartCoroutine(Init());
            Dispatcher.Subscribe(EventId.ServerDataReceived, PopulatePrices);
        }

        private void Start()
        {
            AddSubscriber(StaticType.UI.Instance());
        }

        private void OnDestroy()
        {
            RemoveSubscriber(StaticType.UI.Instance());
        }

        private void PopulatePrices(EventId arg1, EventInfo arg2)
        {
            foreach (var socialPrice in GameData.socialPrices)
            {
                var price = new SocialPrice(Convert.ToInt32(socialPrice.Value), SocialCurrency.Ok);
                items.Add(new SocialGoods(socialPrice.Key, Localizer.GetText(socialPrice.Key), price));
            }
        }

        private IEnumerator Init()
        {
            GetAppParameters();
            GetProfileInfo();
            while (!app_parameters_loaded || !profile_loaded)
            {
                yield return new WaitForSeconds(1f);
            }
            GetFriendsAppUsers();
            while (!friends_loaded)
                yield return null;
            LoginSucceed(this);
        }

        private IEnumerator updateSocialActivity()
        {
            bool joined = ProfileInfo.socialActivity.Contains(SocialAction.joined);
            bool invited = ProfileInfo.socialActivity.Contains(SocialAction.invited);
            bool shared = ProfileInfo.socialActivity.Contains(SocialAction.shared);

            if (!joined)
                yield return StartCoroutine(CheckGroupMembership(delegate(bool isMember)
                {
                    if (isMember)
                    {
                        StaticType.SocialSettings.Instance<ISocialSettings>().ReportSocialActivity(SocialAction.joined);
                        joined = true;
                    }
                }));

            var info = new Dictionary<string, object> {{"shared", shared}, {"invited", invited}, {"joined", joined}};
            string eval = string.Format("updateSocialActivity({0});", Json.Serialize(info));
            Application.ExternalEval(eval);
        }

        public IEnumerator CheckGroupMembership(Action<bool> OnComplete)
        {
            var parameters = new Dictionary<string, object>
            {
                {"method", "group.getUserGroupsByIds"},
                {"uids", UserId},
                {"group_id", GameData.socialGroups["ok"]}
            };
            bool finished = false;
            callByCallbackAndParams("FAPI.Client.call", Json.Serialize(parameters),
                delegate(object o, Callback callback)
                {
                    string[] good_statuses = {"ACTIVE", "BLOCKED", "MODERATOR", "ADMIN"};
                    string status = new JsonPrefs((string) o).ValueString("data/0/status", "UNKNOWN");
                    bool is_member = good_statuses.Contains(status);
                    OnComplete(is_member);
                    finished = true;
                });
            while (!finished)
                yield return null;
        }

        public void callByCallbackAndParams(string functionName, string parameters, Action<object, Callback> action)
        {
#if UNITY_WEBPLAYER || UNITY_WEBGL
            Callback callback = callbackPool.getCallback(CallbackType.DISPOSABLE);
            callback.action = action;
            string eval = @"
			container._callbackCALLBACK_ID = function (status, data, error){
				var params = {
					""status"": status,
					""data"": data,
					""error"": error
                };
				container.callback(CALLBACK_ID, JSON.stringify(params));
			}
			FUNCTION_NAME(ADDITIONAL_PARAMETERS, container._callbackCALLBACK_ID);"
                .Replace("CALLBACK_ID", "" + callback.id)
                .Replace("FUNCTION_NAME", functionName)
                .Replace("ADDITIONAL_PARAMETERS", parameters);


            LogToConsole("callByCallbackAndParams external evaluation: \n" + eval);
            Application.ExternalEval(eval);
#endif
        }

#region ISender
        public string Description
        {
            get
            {
                return "[Bank] " + name;
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

        public void Event(XD.Message message, params object[] _parameters)
        {
            for (int i = 0; i < Subscribers.Count; i++)
            {
                Subscribers[i].Reaction(message, _parameters);
            }
        }
 #endregion
    }
}
