using System.Linq;
using Tanks.Models;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using MiniJSON;
using System.Text;

[RequireComponent(typeof(CallbackPool))]
public class Moimir : MonoBehaviour, ISocialService
{
    public string UserId{ get{ return app_params.ContainsKey("vid") ? app_params["vid"] as string : ""; }}
    public string SessionKey{ get{ return app_params.ContainsKey("session_key") ? app_params["session_key"] as string : ""; }}
    public string AuthSig{ get{ return app_params.ContainsKey("sig") ? app_params["sig"] as string : ""; }}
#pragma warning disable 67
    public event Action<ISocialService, SocialInitializationState> InitializationFinished = delegate { };
    public event Action<ISocialService> LoginSucceed = delegate { };
    public event Action LoginFail = delegate { };
    public event Action LogoutSucceed = delegate { };
    public event Action<string> GroupJoined = delegate { };
    public event Action GroupJoinErrorOccured = delegate { };
#pragma warning restore 67
    public bool IsLoggedIn { get; private set; }
    public bool IsInitialized { get; private set; }
    public SocialNetworkInfo SocialNetworkInfo
    {
        get
        {
            return new SocialNetworkInfo(SocialPlatform.Mail, "Mail", "");
        }
    }
    public Player Player { get; private set; }

    public void Login()
    {
        throw new NotImplementedException();
    }

    public void Logout()
    {
        throw new NotImplementedException();
    }

    public string Uid()
    {
        return UserId;
    }

    public string AvatarUrl()
    {
        return profile_info.ContainsKey ("pic_small") ? profile_info ["pic_small"] as string : "";
    }

    public void ShowPayment(string item_id)
    {
        if (Screen.fullScreen)
            Screen.SetResolution(960, 600, false);
        Application.ExternalCall("MMAPIWrapper.unity_hide");
        LogToConsole("ShowPayment " + itemPriceLabels[item_id]);
        Application.ExternalCall("MMAPIWrapper.payment", JSONFormat(itemPriceLabels[item_id]));
    }

    public void InviteFriend()
    {
        LogToConsole("InviteFriend");
        if(Screen.fullScreen)
            Screen.SetResolution(960,600,false);
        Application.ExternalCall("MMAPIWrapper.unity_hide");
        Application.ExternalCall("MMAPIWrapper.invite", "{\"text\": \"Поиграй со мной в эту игру!!\"}");
    }

    public SocialPrice GetPriceById(string item_id)
    {
        return itemPriceLabels[item_id].price;
    }

    public string GetPriceStringById (string item_id)
    {
        return GetPriceById (item_id).PriceString;
    }
    public void PostNewLevelToWall()
    {
        if (Screen.fullScreen)
            Screen.SetResolution(960, 600, false);
        Application.ExternalCall("MMAPIWrapper.new_level", ProfileInfo.Level);
    }

    public void Post(string text, Texture2D img)
    {
        XdevsSplashScreen.SetActiveWaitingIndicator(true);
        UploadImage(img, delegate(string img_url)
        {
            XdevsSplashScreen.SetActiveWaitingIndicator(false);
            if (Screen.fullScreen)
                Screen.SetResolution(960, 600, false);
            var parameters = new Dictionary<string, object>
            {
                {"title", Application.productName },
                {"text", text + " http://my.mail.ru/apps/" + app_params["app_id"]},
                {"img_url", img_url}
            };
            callMailruByObjectMailruListenerAndCallback(
                "mailru.common.stream.post", parameters,
                "mailru.common.events.streamPublish",
                delegate(object obj, Callback cbk)
                {
                    var prefs = new JsonPrefs(obj);
                    var status = prefs.ValueString("status");
                    if (!status.Equals("opened"))//status.Equals("uploadSuccess") || status.Equals("closed"))
                    {
                        CallbackPool.instance.releasePermanentCallback(cbk);
                    }
                });
        }, delegate(string error_text)
        {
            XdevsSplashScreen.SetActiveWaitingIndicator(false);
            if (Screen.fullScreen)
                Screen.SetResolution(960, 600, false);
            LogToConsole(error_text);
        });
    }

    private void UploadImage(Texture2D img, Action<string> OnSuccess, Action<string> OnFail)
    {
        var request = Http.Manager.Instance().CreateRequest("/upload/screenshot");
        request.Form.AddField("image", Convert.ToBase64String(img.EncodeToJPG()));
        Http.Manager.StartAsyncRequest(request, delegate(Http.Response result)
        {
            string img_url = new JsonPrefs(result.text).ValueString("url");
            //img_url = "http://tesdh.esy.es/0b2893eba2aadc2db64de5492236ed26.png";
            if (!string.IsNullOrEmpty(img_url))
            {
                var parameters = new Dictionary<string, object>
                {
                    {"url", img_url},
                    {"aid", "_myphoto"}
                };
                callMailruByObjectMailruListenerAndCallback(
                    "mailru.common.photos.upload", parameters,
                    "mailru.common.events.upload",
                    delegate(object obj, Callback cbk)
                    {
                        var prefs = new JsonPrefs(obj);
                        if (prefs.ValueString("status").Equals("uploadSuccess"))
                        {
                            CallbackPool.instance.releasePermanentCallback(cbk);
                            callByCallbackAndParams("mailru.common.photos.get", delegate(object arg1, Callback arg2)
                            {
                                LogToConsole(arg1.ToString());
                                string uploaded_img_url = new JsonPrefs(arg1).ValueString("0/src");
                                if (!string.IsNullOrEmpty(uploaded_img_url))
                                {
                                    OnSuccess(uploaded_img_url);
                                }
                                else
                                {
                                    OnFail("img_url on mailru server is empty");
                                }
                            }, "_myphoto", prefs.ValueString("pid"));
                        }
                        else if (prefs.ValueString("status").Equals("closed"))
                        {
                            CallbackPool.instance.releasePermanentCallback(cbk);
                            OnFail("Confirmation dialog closed by user");
                        }
                    });
            }
            else
            {
                OnFail("Empty img_url");
            }
        }, delegate(Http.Response result)
        {
            OnFail("/upload/screenshot failed? result - " + result.text);
        });
    }
    public void PostAchievement(AchievementsIds.Id webAchievement, string text)
    {
        if (Screen.fullScreen)
            Screen.SetResolution(960, 600, false);
        Application.ExternalCall("MMAPIWrapper.achievement_report", webAchievement, text + " http://my.mail.ru/apps/" + app_params["app_id"]);
    }

    public List<string> GetFriendsAppUsersIds()
    {
        List<string> friends_uids = new List<string>();
        foreach (Dictionary<string,object> friend in friends)
        {
            friends_uids.Add((string)friend["uid"]);
        }
        return friends_uids;
    }

    public void UpdateSocialActivity() { }
    public void JoinGroup(string id)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<SocialNetworkGroup> AllSocialGroups { get { return new List<SocialNetworkGroup>(); } }

    public Dictionary<string,string> GetAuthParams()
    {
        StringBuilder sb = new StringBuilder ();
        SortedDictionary<string,object> app_params_temp = new SortedDictionary<string, object>(app_params);
        app_params_temp.Remove("sig");
        foreach(var v in app_params_temp)
            sb.AppendFormat("{0}={1}", v.Key, v.Value);
        return new Dictionary<string, string> (){
            {"mailru",UserId},
            {"signature", AuthSig},
            {"params",sb.ToString()}
        };
    }

    CallbackPool callbackPool;
    private Dictionary<string,object> app_params = new Dictionary<string, object>();
    private Dictionary<string,object> profile_info = new Dictionary<string, object>();
    private List<object> friends = new List<object>();
    private bool app_parameters_loaded;
    private bool profile_loaded;
    private bool friends_loaded;

    public void GetFriendsAppUsers()
    {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        callByCallbackAndParams("mailru.common.friends.getAppUsers", delegate(object obj, Callback callback)
        {
            friends = obj as List<object>;
            if (obj as List<object> != null)
            {
                friends = obj as List<object>;
            }
            friends.Add(new Dictionary<string, object>(){
                {"uid",UserId},
                {"first_name", profile_info["first_name"]},
                {"pic_50", profile_info["pic_50"]}
            });
            friends_loaded = true;
        }, true);
#else
        friends_loaded = false;
#endif
    }

    void Awake()
    {
        Messenger.Subscribe(EventId.ServerDataReceived, PopulatePrices);
        callbackPool = CallbackPool.instance;
        callbackPool.initialize();
        StartCoroutine(Init());
    }
    private void PopulatePrices(EventId arg1, EventInfo arg2)
    {
        foreach (var socialPrice in GameData.socialPrices)
        {
            var prefs = new JsonPrefs(socialPrice.Value);
            var price = new SocialPrice(Convert.ToInt32(prefs.ValueInt("price")), SocialCurrency.Mailik);
            var id = prefs.ValueString("id");
            itemPriceLabels.Add(socialPrice.Key, new SocialGoods(id, Localizer.GetText(socialPrice.Key), price));
        }
    }

    IEnumerator Init()
    {
        GetAppParameters();
        GetProfileInfo();
        InstallEventListener("app.incomingPayment", "PaymentCallback");
        InstallEventListener("app.friendsInvitation", "FriendInvitationCallback");
        InstallEventListener("app.paymentDialogStatus", "PaymentDialogStatus");
        while (!app_parameters_loaded || !profile_loaded)
            yield return null;
        GetFriendsAppUsers();
        while (!friends_loaded)
            yield return null;
        LoginSucceed(this);
    }

    void GetAppParameters()
    {
        Application.ExternalCall("MMAPIWrapper.get_app_params", gameObject.name, "GetAppParametersCallback");
    }

    void GetProfileInfo()
    {
        Application.ExternalCall("MMAPIWrapper.get_profile_info",gameObject.name,"GetProfileInfoCallback");
    }

    void GetAppParametersCallback(string result)
    {
        LogToConsole("GetAppParametersCallback result\n"+result);
        app_params = Json.Deserialize(result) as Dictionary<string,object>;
        app_parameters_loaded = true;
    }

    void GetProfileInfoCallback(string result)
    {
        LogToConsole("GetProfileInfoCallback result\n"+result);
        profile_info = Json.Deserialize(result) as Dictionary<string,object>;
        profile_loaded = true;
    }
    void InstallEventListener(string event_name, string callback)
    {
        LogToConsole("Installing event listener for " + event_name);
        Application.ExternalCall("MMAPIWrapper.install_event_listener", gameObject.name, callback, event_name);
    }
    string JSONFormat(SocialGoods item)
    {
        Dictionary<string, object> dict = new Dictionary<string, object> (){
            {"service_id", item.id},
            {"service_name", item.name},
            {"mailiki_price", item.price.Value}
        };
        return Json.Serialize(dict);
    }

    private Dictionary<string, SocialGoods> itemPriceLabels = new Dictionary<string, SocialGoods>();

    void PaymentCallback(string result)
    {
        LogToConsole("PaymentCallback " + result);
        Dictionary<string,object> dict = Json.Deserialize(result) as Dictionary<string,object>;
        if((string)dict["status"] == "success")
        {
            ProfileInfo.SaveToServer();
        }
    }

    void PaymentDialogStatus(string ev)
    {
        var status = new JsonPrefs(ev).ValueString("status");
        if (status.Equals("closed"))
        {
            Application.ExternalCall("MMAPIWrapper.unity_show");
            XdevsSplashScreen.SetActiveWaitingIndicator(false);
        }
        
    }
    void FriendInvitationCallback(string result)
    {
        LogToConsole("FriendInvitationCallback "+result);
        Dictionary<string,object> dict = Json.Deserialize(result) as Dictionary<string,object>;
        if(dict.ContainsKey("status") && (string)dict["status"] == "closed") {
            Application.ExternalCall("MMAPIWrapper.unity_show");
        }
        if(dict.ContainsKey("data")) {
            if(SocialSettings.IsBonusForInviteAvailable) {
                Http.Manager.FuelForInvite((fResult, response) => {
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
    }

    void callByCallbackAndParams(string functionName, Action<object, Callback> action, params object[] parameters)
    {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        Callback callback = callbackPool.getCallback(CallbackType.DISPOSABLE);
        callback.action = action;
        string initialization="";
        string functionParams="";
        for (int i = 0; i < parameters.Length; i++) {
            initialization+=@"paramNUM=PARAMETER_JSON;"
                .Replace("NUM",""+i)
                .Replace("PARAMETER_JSON",Json.Serialize(parameters[i]));
            functionParams+=", paramNUM".Replace("NUM",""+i);
        }
        string eval= @"
            INITIALIZATION

            container._callbackCALLBACK_ID = function (result){
                container.callback(CALLBACK_ID, result);
            }
            FUNCTION_NAME(container._callbackCALLBACK_ID ADDITIONAL_PARAMETERS);"
        .Replace("INITIALIZATION",initialization)
        .Replace("CALLBACK_ID",""+callback.id)
        .Replace("FUNCTION_NAME", functionName)
        .Replace("ADDITIONAL_PARAMETERS", functionParams);

        LogToConsole("callMailruByCallbackAndParams external evaluation: \n"+eval);
        Application.ExternalEval(eval);
#endif
    }

    public void callMailruByObjectMailruListenerAndCallback(string functionName, object paramsObject, string mailruListenEvent, Action<object, Callback> action)
    {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        Callback callback = callbackPool.getCallback(CallbackType.PERMANENT);
        LogToConsole("callbackType="+callback.type.ToString());
        callback.action = action;
        string eval= @"
            container._callbackCALLBACK_ID = function (result){
                result.originalProps=PARAMS_JSON_STRING;
                container.callback(CALLBACK_ID, result);
            }
            var mailruEventId=mailru.events.listen('MAIL_RU_LISTEN_EVENT', container._callbackCALLBACK_ID);
            container.updateCallbackId(CALLBACK_ID, mailruEventId);
            var params=PARAMS_JSON_STRING;
            FUNCTION_NAME(params);"
            .Replace("MAIL_RU_LISTEN_EVENT", mailruListenEvent)
            .Replace("CALLBACK_ID"         , ""+callback.id)
            .Replace("PARAMS_JSON_STRING"  , 	Json.Serialize(paramsObject))
            .Replace("FUNCTION_NAME"       , functionName);

        LogToConsole("callMailruByObjectMailruListenerAndCallback external evaluation: \n"+eval);

        Application.ExternalEval(eval);
#endif
    }

    void LogToConsole(string param){
        if(Debug.isDebugBuild) {
            Application.ExternalCall("console.log", param);
            Debug.Log(param);
        }
    }
}
