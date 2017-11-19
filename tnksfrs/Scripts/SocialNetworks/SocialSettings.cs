using System.Collections;
using Http;
using Tanks.Models;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using XD;

public enum SocialPlatform
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
public class SocialPrice
{
    public string PriceString { get; private set; }
    public int Value { get; private set; }
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

    public SocialPrice(int _value, SocialCurrency _currency)
    {
        Value = _value;
        Currency = _currency;
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

public interface ISocialService : ISender
{
    event Action<ISocialService, SocialInitializationState> InitializationFinished;
    event Action<ISocialService> LoginSucceed;
    event Action LoginFail;
    event Action LogoutSucceed;
    event Action<string> GroupJoined;
    event Action GroupJoinErrorOccured;
    bool IsLoggedIn{ get; }
    bool IsInitialized { get; }
    SocialNetworkInfo SocialNetworkInfo { get; }
    Tanks.Models.Player Player { get;}
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
    public event Action<ISocialService, SocialInitializationState> InitializationFinished;
    public event Action<ISocialService> LoginSucceed;
    public event Action LoginFail;
    public event Action LogoutSucceed;
    public event Action<string> GroupJoined;
    public event Action GroupJoinErrorOccured;
    public bool IsLoggedIn { get; private set; }
    public bool IsInitialized { get; private set; }
    public SocialNetworkInfo SocialNetworkInfo { get; private set; }
    public Tanks.Models.Player Player { get; private set; }
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
        GUIPager.SetActivePage("Socials", false, GameData.IsGame(Game.FutureTanks | Game.ToonWars | Game.SpaceJet | Game.BattleOfWarplanes | Game.BattleOfHelicopters));
    }

#region ISender
    public string Description
    {
        get
        {
            return "[SocialSettings] ";
        }

        set
        {

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

public interface ISocialSettings : IStatic
{

    /*SocialSettings Instance
    {
        get;
    }*/

    bool IsWebPlatform
    {
        get;
    }

    bool PlatformMoimir
    {
        get;
    }

    bool PlatformOdnoklassniki
    {
        get;
    }

    bool PlatformVkontakte
    {
        get;
    }

    bool PlatformFacebook
    {
        get;
    }

    bool PlatformUndefined
    {
        get;
    }

    bool IsHangarReloadRequired
    {
        get;
        set;
    }

    Action OnLastInviteChanged
    {
        get;
        set;
    }

    ISocialService GetSocialService();

    SocialPlatform CurrentRegionSocial
    {
        get;
    }

    bool IsLoggedIn
    {
        get;
    }

    void ReportSocialActivity(SocialAction action);
    event Action LoginSucceed;
}

public class SocialSettings : MonoBehaviour, ISocialSettings
{
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
            return StaticType.SocialSettings;
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

    #region ISender
    public string Description
    {
        get
        {
            return "[SocialSettings] " + name;
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

    public void Event(XD.Message message, params object[] parameters)
    {
        for (int i = 0; i < Subscribers.Count; i++)
        {
            Subscribers[i].Reaction(message, parameters);
        }
    }
    #endregion

    #region ISubscriber       
    public void Reaction(XD.Message message, params object[] parameters)
    {
        switch (message)
        {
            case XD.Message.DataInited:
                dataInited = true;
                break;
            case XD.Message.PlatformScreenshotPost:
                ISocialService service = StaticType.SocialSettings.Instance<ISocialSettings>().GetSocialService();
                if (service == null || service.SocialNetworkInfo == null || 
                    (service.SocialNetworkInfo.Platform != SocialPlatform.Facebook &&
                     service.SocialNetworkInfo.Platform != SocialPlatform.Odnoklassniki &&
                     service.SocialNetworkInfo.Platform != SocialPlatform.Vkontakte))
                {
                    #if DEBUG_TAG_TF
                    Debug.Log("TF: Not Loggined social service.");
                    #endif
                    Event(XD.Message.LayoutRequest, PSYWindow.Social);
                }
                break;
        }
    }
#endregion

    public static SocialPlatform Platform { get { return platform; } }

    public SocialPlatform buildPlatform = SocialPlatform.Undefined;

    public event Action LogoutSucceed = delegate {  };
    public event Action LoginSucceed = delegate {  };
    public bool IsWebPlatform
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

    public bool PlatformMoimir { get { return platform == SocialPlatform.Mail; } }
    public bool PlatformOdnoklassniki { get { return platform == SocialPlatform.Odnoklassniki; } }
    public bool PlatformVkontakte { get { return platform == SocialPlatform.Vkontakte; } }
    public bool PlatformFacebook { get { return platform == SocialPlatform.Facebook; } }
    public bool PlatformUndefined { get { return platform == SocialPlatform.Undefined; } }
    public bool IsHangarReloadRequired { get; set; }
    public Action OnLastInviteChanged { get; set; }

    private static SocialSettings instance;
    private static SocialPlatform platform = SocialPlatform.Undefined;
    private ISocialService service = new SocialService();

    public const int IDLE_THRESHOLD = 5 * 60; // Seconds.

    public ISocialService GetSocialService()
    {
        return service;
    }

    public SocialPlatform CurrentRegionSocial
    {
        get
        {
            if(StaticType.Localization.Instance<ILocalization>().CurrentLanguage == SystemLanguage.Russian)
            {
                return SocialPlatform.Vkontakte;
            }
            else
            {
                return SocialPlatform.Facebook;
            }
        }
    }

    private bool dataInited = false;

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
#if DEBUG_FOR_WEB_GL
				Debug.LogError("TF: SocialSettings.Awake: SetPlatform: service = (ISocialService)gameObject.AddComponent<Moimir>();");
#endif
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
            return false;
        }
    }

    //   StaticType.SocialSettings.Instance<ISocialSettings>().IsLoggedIn
    public bool IsLoggedIn
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

    private void Awake ()
    {
		gameObject.name = "SocialSettings";
		SaveInstance();
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);

            gameObject.AddComponent<EMail>();
#if UNITY_EDITOR && (UNITY_WEBPLAYER || UNITY_WEBGL)
            buildPlatform = SocialPlatform.Facebook;
            Dispatcher.Send(EventId.AfterWebPlatformDefined, null);    
#endif

#if UNITY_WEBPLAYER || UNITY_WEBGL
            gameObject.AddComponent<WebAvatarDownloader>();
#if DEBUG_FOR_WEB_GL
			Debug.LogError("TF: SocialSettings.Awake: Application.ExternalEval (jsPlatformDetector);");
#endif
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

    private void Start()
    {
        StaticType.UI.AddSubscriber(this);
        AddSubscriber(StaticType.UI.Instance());
    }

    private void OnDestroy()
    {
        RemoveSubscriber(StaticType.UI.Instance());
        StaticType.UI.RemoveSubscriber(this);
        DeleteInstance();
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnSocialLogout()
    {
        service = new SocialService();
        platform = SocialPlatform.Undefined;
        LogoutSucceed();
        IsHangarReloadRequired = true;
        Event(XD.Message.PlatformUserLogout);
        Loading.GoToLoadingScene();
        StaticType.UI.RemoveSubscriber(this);
    }

    private void OnSocialInitialized(ISocialService socialService, SocialInitializationState initializationState)
    {
        //Debug.Log("TF: SocialSettings: OnSocialInitialized: socialService == " + socialService + "; initializationState == " + initializationState.ToString());
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
            //Debug.Log("TF: SocialSettings.OnSocialInitialized. if (initialized && !loggedIn).Dispatcher.Send(EventId.AfterWebPlatformDefined, ");
            Dispatcher.Send(EventId.AfterWebPlatformDefined, new EventInfo_SimpleEvent());
        }
    }

    private void OnSocialLogin(ISocialService service)
    {
        Debug.Log("TF: SocialSettings - OnSocialLogin");
        platform = service.SocialNetworkInfo.Platform;
#if !(UNITY_WEBPLAYER || UNITY_WEBGL)
        this.service = service;
#endif
        //Отключаю этот пункт временно, и заменяю на Денисов вызов
        Dispatcher.Send(EventId.AfterWebPlatformDefined, null);
        LoginSucceed();
        if (!XD.StaticContainer.SceneManager.InBattle)
        {
            StartCoroutine(SaveToServer());
        }
    }
    
    private IEnumerator SaveToServer()
    {
        while (!dataInited)
        {
            yield return null;
        }

        ProfileInfo.SaveToServer(OnFinishCallback);
        yield return null;
    }

    private void OnFinishCallback(Response response, bool result)
    {
        if (GUIPager.ActivePage == "Settings")
        {
            Settings.SetFirstSettingsTab();
        }
        //Выяснить что это и зачем. 2 августа 2017 Василий.
        /*if (GameData.isBonusForSocialActivityEnabled)
        {
            Debug.Log("TF: SocialSettings - OnFinishCallback 4");
            SetupSocialActivityPanel();
            Debug.Log("TF: SocialSettings - OnFinishCallback 5");
            service.UpdateSocialActivity();
            Debug.Log("TF: SocialSettings - OnFinishCallback 6");
        }*/
        Event(XD.Message.PlatformUserLoginned);
    }

    private void OnActiveSceneChanged(Scene previousScene, Scene newScene)
    {
		//Отключено до выяснения причин ошибок которые возникают в контакте из-за этих строкюВасилий 10 августа 2017
        /*if (!XD.StaticContainer.SceneManager.InBattle)
        {
            if (GameData.isBonusForSocialActivityEnabled)
            {
                SetupSocialActivityPanel();
                service.UpdateSocialActivity();
            }
        }*/
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
        string eval = @"
            $('#{4}').css('visibility', 'hidden');
            $(""#dialog-openurl"").remove();
			$(""body"").append(""<div id='dialog-openurl'><p><span class='ui-icon ui-icon-alert'></span>{0}</p></div>"");
			var dialog = $(""#dialog-openurl"").dialog({{
				dialogClass: ""no-close"",
				draggable: false,
				autoOpen: false,
				resizable: false,
                position: {{ my: ""center"", at: ""center"", of: $(""#{4}"") }},
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
