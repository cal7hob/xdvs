using System.Collections;
using Http;
using Tanks.Models;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public enum SocialPlatform //Не переименовывать, завязаны имена спрайтов
{
    Undefined,
    Facebook,
    Vkontakte,
    Odnoklassniki,
    Mail
}

public enum SocialCurrency
{
    Mailik,
    Ok,
    Vote,
    Dollar
}
public enum SocialAction
{
    bookmarked,
    joined,
    shared,
    invited,
    activated
}

public enum SocialInitializationState
{
    Success,
    Error
}

public enum SocialNetworkIconType //Не переименовывать, завязаны имена спрайтов
{
    socialButton,
    linkPanel,
}

public class SocialPrice
{
    public string PriceString { get; private set; }
    public float Value { get; private set; }
    public SocialCurrency Currency { get; private set; }

    Dictionary<SocialCurrency, string> CurrencyToString = new Dictionary<SocialCurrency, string>(){
		{SocialCurrency.Mailik, "мэйликов"},
		{SocialCurrency.Ok, "ОК"},
		{SocialCurrency.Vote, "голосов"},
        {SocialCurrency.Dollar, "$"}
	};

    Dictionary<SocialCurrency, string> CurrencyFormat = new Dictionary<SocialCurrency, string>(){
		{SocialCurrency.Mailik, "{0} {1}"},
		{SocialCurrency.Ok, "{0} {1}"},
		{SocialCurrency.Vote, "{0} {1}"},
        {SocialCurrency.Dollar, "{1}{0}"},
	};

    public SocialPrice(float value, SocialCurrency currency)
    {
        Value = value;
        Currency = currency;
        PriceString = string.Format(CurrencyFormat[Currency], Value, CurrencyToString[Currency]);
    }
}

public class SocialGoods
{
    public string id;
    public string name;
    public SocialPrice price;
    public SocialGoods(string _id, string _name, SocialPrice _price)
    {
        id = _id;
        name = _name;
        price = _price;
    }
};

public interface ISocialService
{
    event Action<ISocialService, SocialInitializationState> InitializationFinished;
    event Action<ISocialService> LoginSucceed;
    event Action LoginFail;
    event Action LogoutSucceed;
    event Action<string> GroupJoined;
    event Action GroupJoinErrorOccured;
    bool IsLoggedIn{ get; }
    bool IsInitialized { get; }
    SocialPlatform Platform { get; }
    Player Player { get;}
    void Login();
    void Logout();
    string Uid();
    string AvatarUrl();
    void InviteFriend();
    void ShowPayment(string item_id);
    Dictionary<string, string> GetAuthParams();
    SocialPrice GetPriceById(string item_id);
    string GetPriceStringById(string item_id);
    void PostNewLevelToWall();
    void Post(string text, Texture2D img);
    void PostAchievement(AchievementsIds.Id webAchievement, string text);        //Пост ачивок в соц сети при получении
    List<string> GetFriendsAppUsersIds();
    void UpdateSocialActivity();
    void JoinGroup(string id);
    IEnumerable<SocialNetworkGroup> AllSocialGroups { get; }
}

class SocialService : ISocialService
{
#pragma warning disable 67
    public event Action<ISocialService, SocialInitializationState> InitializationFinished;
    public event Action<ISocialService> LoginSucceed;
    public event Action LoginFail;
    public event Action LogoutSucceed;
    public event Action<string> GroupJoined;
    public event Action GroupJoinErrorOccured;
#pragma warning restore 67
    public bool IsLoggedIn { get; private set; }
    public bool IsInitialized { get; private set; }
    public SocialPlatform Platform { get { return platform; } private set { platform = value; }}
    public Player Player { get; private set; }

    private SocialPlatform platform = SocialPlatform.Undefined;

    public void Login() { }
    public void Logout() { }
    public string Uid() { return string.Empty; }
    public string AvatarUrl() { return string.Empty; }
    public void InviteFriend()
    {
        OpenSettingsSocialTab();
    }
    public void ShowPayment(string item_id) { }
    public Dictionary<string, string> GetAuthParams() { return new Dictionary<string, string>(); }
    public SocialPrice GetPriceById(string item_id) { return new SocialPrice(1, SocialCurrency.Dollar); }
    public string GetPriceStringById(string item_id) { return string.Empty; }
    public void PostNewLevelToWall()
    {
        OpenSettingsSocialTab();
    }
    public void Post(string text, Texture2D img)
    {
        OpenSettingsSocialTab();
    }
    public void PostAchievement(AchievementsIds.Id webAchievement, string text)
    {
        OpenSettingsSocialTab();
    }
    public List<string> GetFriendsAppUsersIds() { return new List<string>(); }
    public void UpdateSocialActivity() { }
    public void JoinGroup(string id) { }
    public IEnumerable<SocialNetworkGroup> AllSocialGroups { get; private set; }

    private void OpenSettingsSocialTab()
    {
        LinkAccountPage.OpenPageStatic();
    }
}

public class SocialSettings : MonoBehaviour
{
    public SocialPlatform buildPlatform = SocialPlatform.Undefined;

    public static SocialSettings Instance
    {
        get
        {
            if (instance == null)
            {
                instance = GameObject.FindObjectOfType<SocialSettings>();
            }
            return instance;
        }
    }

    public static SocialPlatform Platform {get{return platform;}}
    private static SocialPlatform platform = SocialPlatform.Undefined;

    public event Action LogoutSucceed = delegate {  };
    public event Action LoginSucceed = delegate {  };
    public static bool IsWebPlatform
    {
        get
        {
#if UNITY_WEBPLAYER || UNITY_WEBGL
            return true;
#else
            return false;
#endif
        }
    }

    public static bool PlatformMoimir { get { return platform == SocialPlatform.Mail; } }
    public static bool PlatformOdnoklassniki { get { return platform == SocialPlatform.Odnoklassniki; } }
    public static bool PlatformVkontakte { get { return platform == SocialPlatform.Vkontakte; } }
    public static bool PlatformFacebook { get { return platform == SocialPlatform.Facebook; } }
    public static bool PlatformUndefined { get { return platform == SocialPlatform.Undefined; } }
    public static bool IsHangarReloadRequired { get; set; }
    public static Action OnLastInviteChanged;

    /// <summary>
    /// Помнится тут кто то хотел сделать локализацию названий соцсетей...
    /// </summary>
    public static Dictionary<SocialPlatform, string> platformToName = new Dictionary<SocialPlatform, string>()
    {
        {SocialPlatform.Facebook, "Facebook"},
        {SocialPlatform.Vkontakte, "ВКонтакте"},
        {SocialPlatform.Odnoklassniki, "Одноклассники"},
        {SocialPlatform.Mail, "Mail"},
        {SocialPlatform.Undefined, "Undefined"},
    };

    private static SocialSettings instance;
    private ISocialService service = new SocialService();

    public const int IDLE_THRESHOLD = 5 * 60; // Seconds.

    public static ISocialService GetSocialService() { return Instance.service; }

    public void SetPlatform(string platform_js)
    {
        platform = (SocialPlatform)Enum.Parse(typeof(SocialPlatform), platform_js);
        
        switch (platform)
        {
            case SocialPlatform.Facebook:
                service = (ISocialService)gameObject.AddComponent<SocialNetworks.Facebook>();
                break;
            case SocialPlatform.Odnoklassniki:
                service = (ISocialService)gameObject.AddComponent<SocialNetworks.Odnoklassniki>();
                break;
            case SocialPlatform.Vkontakte:
                service = (ISocialService)gameObject.AddComponent<VKManager>();
                break;
            case SocialPlatform.Mail:
                service = (ISocialService)gameObject.AddComponent<Moimir>();
                break;
        }

        service.InitializationFinished += OnSocialInitialized;
        service.LoginSucceed += OnSocialLogin;
        service.LogoutSucceed += OnSocialLogout;
    }

    public static bool IsBonusForInviteAvailable
    {
        get
        {
            if (HangarController.Instance == null)
            {
                return false;
            }
            return ProfileInfo.isFuelForInviteAvailable;
        }
    }

    public static bool IsLoggedIn
    {
        get { return platform != SocialPlatform.Undefined; } 
    }

#if UNITY_WEBPLAYER || UNITY_WEBGL
    private readonly static string jsPlatformDetector = @"
        container.PlatformDetector = function () {
            try {
                if (document.getElementById('fb-root')) {
                    return 'Facebook';
                }
            } catch(e){}
            try {
                if (VK) {
                    return 'Vkontakte';
                }
            } catch(e){}
            try {
                if (mailru) {
                    return 'Mail';
                }
            } catch(e){}
            try {
                if (OKAPIWrapper) {
                    return 'Odnoklassniki';
                }
            } catch(e){}
            return 'Undefined';
        }";
#endif

    private List<ISocialService> services = new List<ISocialService>();

    void Start ()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
#if UNITY_EDITOR && (UNITY_WEBPLAYER || UNITY_WEBGL)
            buildPlatform = SocialPlatform.Facebook;
            Dispatcher.Send(EventId.AfterWebPlatformDefined, null);    
#endif

#if UNITY_WEBPLAYER || UNITY_WEBGL
            gameObject.AddComponent<WebAvatarDownloader>();
            Application.ExternalEval (jsPlatformDetector);
#if UNITY_WEBPLAYER
            Application.ExternalEval(@"u.getUnity().SendMessage('" + gameObject.name + @"', 'SetPlatform', container.PlatformDetector());");
#else
            Application.ExternalEval (@"
                        try{
                            SendMessage('" + gameObject.name + @"', 'SetPlatform', container.PlatformDetector());
                            console.log(''+container.PlatformDetector());
                        }catch(e){}");
#endif
#elif !UNITY_WP8
            buildPlatform = SocialPlatform.Facebook;
            gameObject.AddComponent<CallbackProcessor>();
#if UNITY_ANDROID || UNITY_IOS || UNITY_WP_8_1
            services.Add(gameObject.AddComponent<VkMobile>());
            services.Add(gameObject.AddComponent<SocialNetworks.Facebook>());
    #if !(UNITY_WP_8_1 || UNITY_IOS)
            services.Add(gameObject.AddComponent<SocialNetworks.OkMobile>());
    #endif
#elif (UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN) && !UNITY_EDITOR
            Dispatcher.Send(EventId.AfterWebPlatformDefined, null);        
#else
            services.Add(gameObject.AddComponent<SocialNetworks.Facebook>());
#endif
            foreach (var socialService in services)
            {
                socialService.InitializationFinished += OnSocialInitialized;
                socialService.LoginSucceed += OnSocialLogin;
                socialService.LogoutSucceed += OnSocialLogout;
            }
#endif

            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }
        else
        {
            if (instance != this)
                Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnSocialLogout()
    {
        service = new SocialService();
        platform = SocialPlatform.Undefined;
        LogoutSucceed();
        IsHangarReloadRequired = true;
        Loading.gotoLoadingScene();
    }

    private void OnSocialInitialized(ISocialService socialService, SocialInitializationState initializationState)
    {
        bool initialized = true;
        bool loggedIn = false;
        if (initializationState == SocialInitializationState.Error)
        {
            services.Remove(socialService);
        }
        foreach (var service in services)
        {
            initialized &= service.IsInitialized;
            loggedIn |= service.IsLoggedIn;
        }
        if (initialized && !loggedIn)
        {
            Dispatcher.Send(EventId.AfterWebPlatformDefined, new EventInfo_SimpleEvent());
        }
    }

    private void OnSocialLogin(ISocialService service)
    {
        platform = service.Platform;
#if !(UNITY_WEBPLAYER||UNITY_WEBGL)
        this.service = service;
#endif
        Dispatcher.Send(EventId.AfterWebPlatformDefined, null);
        LoginSucceed();
        if (GameData.IsHangarScene)
        {
            ProfileInfo.SaveToServer(OnFinishCallback);
        }
    }

    private void OnFinishCallback(Response response, bool result)
    {
        if (GameData.isBonusForSocialActivityEnabled)
        {
            SetupSocialActivityPanel();
            service.UpdateSocialActivity();
        }
    }

    private void OnActiveSceneChanged(Scene previousScene, Scene newScene)
    {
        if (GameData.IsHangarScene)
        {
            if (GameData.isBonusForSocialActivityEnabled)
            {
                SetupSocialActivityPanel();
                service.UpdateSocialActivity();
            }
        }
    }
    private void SetupSocialActivityPanel()
    {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        var price = GameData.awardForSocialActivity;
        Application.ExternalEval(string.Format("setupSocialActivityPanel('{0}','{1}');", price.value, price.currency));
#endif
    }
    public void ReportSocialActivity(SocialAction action)
    {
        var request = Http.Manager.Instance().CreateRequest("/player/socialActivity");
        request.Form.AddField("action", action.ToString());
        Http.Manager.StartAsyncRequest(request, delegate(Response result)
        {
            ProfileInfo.SaveToServer(delegate
            {
                ProfileInfo.socialActivity.Add(action);
                service.UpdateSocialActivity();
            });
        });
    }
    public void ReportSocialActivity(string action)
    {
        try
        {
            var socialAction = (SocialAction)Enum.Parse(typeof(SocialAction), action);
            ReportSocialActivity(socialAction);
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }
    
    public static IEnumerable<ISocialService> AvailableSocialServices()
    {
        return instance.services;
    }

    public static string GetSocialNetworkLogo(SocialPlatform p, SocialNetworkIconType iconType)
    {
        return string.Format("{0}_{1}", iconType, p.ToString().ToLower());
    }

    public static string GetCurPlatformSocialNetworkLogo(SocialNetworkIconType iconType)
    {
        return GetSocialNetworkLogo(Platform, iconType);
    }
}

public class WebTools
{
#if UNITY_WEBGL || UNITY_WEBPLAYER
    public static IEnumerator Jsonp(string url, Dictionary<string,object> data, Action<string> onload, string callback_name = "callback")
    {
        bool finished = false;
        Callback callback = CallbackPool.instance.getCallback(CallbackType.DISPOSABLE);
        callback.action = delegate(object o, Callback callback1)
        {
            finished = true;
            if (null != onload)
                onload((string)o);
        };
        string eval = @"
            $.ajax({
			    url: 'URL',
			    jsonp: 'CBK',
			    dataType: 'jsonp',
			    data: JSON.parse('DATA'),
			    success: function(data) {
				    container.callback(CALLBACK_ID, JSON.stringify(data));
			    }
		    });"
            .Replace("URL", url)
            .Replace("CBK", callback_name)
            .Replace("DATA", MiniJSON.Json.Serialize(data))
            .Replace("CALLBACK_ID", ""+callback.id);
        Application.ExternalEval(eval);
        while (!finished)
            yield return null;
    }

    public static void HideUnity()
    {
        Application.ExternalEval(string.Format("$('#{0}').css('visibility', 'hidden');", Application.isWebPlayer ? "unityPlayer" : "canvas"));
    }

    public static void ShowUnity()
    {
        Application.ExternalEval(string.Format("$('#{0}').css('visibility', 'visible');", Application.isWebPlayer ? "unityPlayer" : "canvas"));
    }

    public static void OpenURL(string url)
    {
        if(Screen.fullScreen)
            Screen.SetResolution(960, 600, false);
        string eval = @"
            $('#{4}').css('visibility', 'hidden');
            $(""#dialog-openurl"").remove();
			$(""body"").append(""<div id='dialog-openurl'><p><span class='ui-icon ui-icon-alert'></span>{0}</p></div>"");
			var dialog = $(""#dialog-openurl"").dialog({{
				dialogClass: ""no-close"",
                closeOnEscape: false,
				draggable: false,
				autoOpen: false,
				resizable: false,
                position: {{ my: ""center"", at: ""left+480 top+300"", of: $(""#{4}"") }},
				width:400,
				modal: true,
				buttons: {{
					""{1}"": function() {{
						window.open(""{3}"", ""_blank"");
						$(this).dialog('close');
                        $('#{4}').css('visibility', 'visible');
					}},
					""{2}"": function() {{
						$(this).dialog(""close"");
                        $('#{4}').css('visibility', 'visible');
					}}
				}}
			}});
			dialog.dialog(""open"");
            dialog.focus();
        ";
        string message = Localizer.GetText("OpenUrlNewTab");
        string button_yes = "OK";
        string button_no = Localizer.GetText("lblCancel");
        eval = string.Format(eval, message, button_yes, button_no, url, Application.isWebPlayer ? "unityPlayer" : "canvas");
        Application.ExternalEval(eval);
    }
#endif
}
