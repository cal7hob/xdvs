using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using Facebook;
using Facebook.Unity;

using HttpMethod = Facebook.Unity.HttpMethod;

public class FacebookManager : MonoBehaviour {

    public class FacebookPermission
    {
        public FacebookPermission(string name, string status)
        {
            Name = name;
            Status = status;
        }
        public FacebookPermission(JsonPrefs data)
        {
            Name = data.ValueString("permission");
            Status = data.ValueString("status");
        }
        public string Name { get; private set; }
        public string Status { get; private set; }
        public static bool operator ==(FacebookPermission a, FacebookPermission b)
        {
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }
            return a.Name == b.Name && a.Status == b.Status;
        }

        public static bool operator !=(FacebookPermission a, FacebookPermission b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            return this == obj as FacebookPermission;
        }

        public override int GetHashCode()
        {
            int hash = 19;
            unchecked
            { // allow "wrap around" in the int
                hash = hash * 31 + this.Name.GetHashCode();
                hash = hash * 31 + this.Status.GetHashCode();
            }
            return hash;
        }
    }

    public FbLoginButton	loginButton;
    public int              scoresReceiveRetries = 3;

    [Serializable]
    public enum Permissions {
        public_profile  = 1,
        email           = 2,
        publish_actions = 3,
        user_friends    = 4
    }

    [Serializable]
    public class Permission {
        public Permissions permission;
        public bool required = true;
    }

    public Permission[] requredPermissions;
    public Permissions  permissionsForLogin;

    static public FacebookManager Instance {get {return s_instance;}}

    static public bool IsBonusForInviteAvailable {
        get { return ProfileInfo.isFuelForInviteAvailable; }
    }
    static public Action OnLastInviteChanged;


    private int             m_scoresReceiveRetries = 0;
    private tk2dTextMesh	m_rewardAmountLabel;
    private bool            m_hangarInitialized = false;
    private bool            m_isInitialized = false;

    //static private string   s_loadMainInfo = "/me?fields=id,first_name,friends{id,first_name}";
    static private string   s_loadMainInfo = "/app/scores?fields=score,user{id,first_name}";

    static private FacebookManager s_instance;

    List<object> fbFriendsObjects = new List<object>();
    private static Dictionary<Game,string> fb_namespaces = new Dictionary<Game, string>{
        {Game.FutureTanks,          "futuretanks"},
        {Game.IronTanks,            "irontanks"},
        {Game.ToonWars,             "toon_wars"},
        {Game.SpaceJet,             "space_jet"},
        {Game.BlowOut,              ""},// TODO: Заменить на реальный.
        {Game.BattleOfWarplanes,    "battleofwarplanes"},
        {Game.WingsOfWar,           ""},// TODO: Заменить на реальный.
        {Game.BattleOfHelicopters,  "battleofhelicopters"},
        {Game.Armada,               "armada_world"},
        {Game.MetalForce,           "metalforce"},
    };

#pragma warning disable 414 // Used uder different defines
    private static Dictionary<Game, string> fb_inviteUrl = new Dictionary<Game, string>{
        {Game.FutureTanks,          "https://fb.me/508622569306233"},
        {Game.IronTanks,            "https://fb.me/1063428703675322"},
        {Game.ToonWars,             "https://fb.me/945382422165788"},
        {Game.SpaceJet,             "https://fb.me/1508684069425204"},
        {Game.BlowOut,              ""},// TODO: Заменить на реальный.
        {Game.BattleOfWarplanes,    "https://fb.me/472142219577009"},
        {Game.WingsOfWar,           ""},// TODO: Заменить на реальный.
        {Game.BattleOfHelicopters,  "https://fb.me/1602058776751290"},
        {Game.Armada,               "https://fb.me/294112947614108"},
        {Game.MetalForce,           "https://fb.me/1763543977290401"}
    };
#pragma warning restore 414

    public static string UserId
    {
        get {
            return AccessToken.CurrentAccessToken != null ? AccessToken.CurrentAccessToken.UserId : "";
        }
    }

    public string AvatarUrl()
    {
        return string.IsNullOrEmpty(UserId) ? "" : "https://graph.facebook.com/" + UserId + "/picture?type=square";
    }

    public void Hide () {
//		Debug.Log ("FacebookManager.hide()");
    }

    public void Show () {
//		Debug.Log ("FacebookManager.show()");
    }

    public static void FriendsInviteRequest () {
        if (!FB.IsInitialized) {
            return;
        }
        if (!FB.IsLoggedIn) {
            Instance.OnFacebookButtonClicked ();
            return;
        }
#if UNITY_IOS || UNITY_ANDROID
        FB.Mobile.AppInvite (new Uri(fb_inviteUrl[GameData.ClearGameFlags(GameData.CurrentGame)]), null, s_instance.AppInviteCallback);
#else
        FB.AppRequest("Come play this great game!", null, null, null, null, null, null, s_instance.FriendsInviteCallback);
#endif
    }

    public static void CheckPermission(FacebookPermission permission, Action<bool> OnComplete)
    {
        FB.API("me/permissions", HttpMethod.GET, delegate(IGraphResult result)
        {
            Debug.Log(result.RawResult);
            var data = new JsonPrefs(result.ResultDictionary).ValueObjectList("data");
            foreach (var o in data)
            {
                var perm = new FacebookPermission(new JsonPrefs(o));
                if (permission == perm)
                {
                    OnComplete(true);
                    return;
                }
            }
            OnComplete(false);
        });
    }
    public static void Post(string text, Texture2D img)
    {
        if (!FB.IsInitialized)
        {
            return;
        }
        if (!FB.IsLoggedIn)
        {
            Instance.OnFacebookButtonClicked();
            return;
        }

        XdevsSplashScreen.SetActiveWaitingIndicator(true);
        MessageBox.Show(MessageBox.Type.Hard, Localizer.GetText("ImageUploading"));

        var form = new WWWForm();
        form.AddBinaryData("image", img.EncodeToPNG(), "screenshot.png");
        form.AddField("published", "false");
        form.AddField("no_story", "true");

        FB.API("me/photos", HttpMethod.POST, delegate(IGraphResult result)
        {
            XdevsSplashScreen.SetActiveWaitingIndicator(false);
            MessageBox.HideHardMessage();
            if (string.IsNullOrEmpty(result.Error))
            {
                FB.API("/" + result.ResultDictionary["id"] + "?fields=source", HttpMethod.GET, delegate(IGraphResult graphResult)
                {
                    if (string.IsNullOrEmpty(graphResult.Error))
                    {
                        FB.ShareLink(
                            new Uri("https://apps.facebook.com/" + fb_namespaces[GameData.ClearGameFlags(GameData.CurrentGame)]),
                            Application.productName,
                            text,
                            new Uri((string)graphResult.ResultDictionary["source"] ),
                            delegate(IShareResult shareResult)
                            {
                                if (shareResult.Cancelled || !string.IsNullOrEmpty(shareResult.Error))
                                    FB.API("/" + result.ResultDictionary["id"], HttpMethod.DELETE);
                            });
                    }
                });
            }
        }, form);
    }


    void Awake () {
        Dispatcher.Subscribe (EventId.AfterHangarInit, StartInit, 1);
#if UNITY_WEBPLAYER || UNITY_WEBGL
        Dispatcher.Subscribe (EventId.AfterWebPlatformDefined, WebPlatformDefined, 1);
#endif
        s_instance = this;
    }

    void OnDestroy () {
        Dispatcher.Unsubscribe(EventId.AfterHangarInit, StartInit);
#if UNITY_WEBPLAYER || UNITY_WEBGL
        Dispatcher.Unsubscribe (EventId.AfterWebPlatformDefined, WebPlatformDefined);
#endif
    }

    // Use this for initialization
    void Start () {
        if (loginButton && loginButton.lFbRewardMoney)
        {
            loginButton.lFbRewardMoney.text = ProfileInfo.SocialActivationReward.LocalizedValue;

            if (loginButton.moneyIcon)
            {
                loginButton.moneyIcon.SetSprite (ProfileInfo.SocialActivationReward.currency.ToString ().ToLower ());
                loginButton.moneyIcon.dimensions = new Vector2 (loginButton.moneyIcon.CurrentSprite.GetBounds ().size.x,
                                                                loginButton.moneyIcon.CurrentSprite.GetBounds ().size.y);
            }
        }

        HideLoginButton ();
#if UNITY_STANDALONE_WIN
        Dispatcher.Send(EventId.AfterFacebookInitialized, new EventInfo_SimpleEvent());
        return;
#elif !(UNITY_WEBPLAYER || UNITY_WEBGL)

        if (!FB.IsInitialized) {
            FB.Init (OnInitComplete, OnHideUnity);
        }
        else {
            OnInitComplete ();
        }
        if (null == loginButton) {
            //Debug.LogError ("You have to assign facebook button object!");
            return;
        }
//        friendsList.SetActive (false);

#endif
    }

    void StartInit (EventId id, EventInfo info) {
        m_hangarInitialized = true;
        OnInitComplete();
    }

    void WebPlatformDefined (EventId id, EventInfo info)
    {
        if (SocialSettings.Platform == SocialPlatform.Facebook) {
            OnInitComplete ();
#if UNITY_EDITOR
            OnFacebookButtonClicked ();
#endif
        }
    }


    private void OnInitComplete()
    {
        if (m_isInitialized) {
            return;
        }
#if !(UNITY_WEBPLAYER || UNITY_WEBGL)
        if (!m_hangarInitialized) {
            Dispatcher.Send (EventId.AfterFacebookInitialized, null);
        }
        if (!m_hangarInitialized || !FB.IsInitialized) {
            return;
        }
#else
        if (!m_hangarInitialized) {
            return;
        }
#endif
        if (null == HangarController.Instance) {
            return;
        }

        m_isInitialized = true;
        if (FB.IsInitialized)
        {
            FB.ActivateApp ();
        }
#if !(UNITY_WEBPLAYER || UNITY_WEBGL)
        if (null != loginButton) {
            loginButton.isActivateMode = IsFirstLogin ();
        }
        ShowLoginButton ();
        if (FB.IsLoggedIn) {
            OnLoggedIn ();
        }
        else if (!IsFirstLogin () && IsAutologinUse ()) {
            // Уже входили, так что пытаемся сразу перелогиниться
            HideLoginButton ();
            OnFacebookButtonClicked ();
        }
#endif

#if UNITY_WEBPLAYER || UNITY_WEBGL
        if (FB.IsLoggedIn) {
            OnLoggedIn ();
        }
        else {
            Debug.LogWarning ("Not logged in Facebook!!!");
        }
#endif
    }

    private void OnHideUnity(bool isGameShown) {
//        Debug.Log("Is game showing? " + isGameShown);
    }

    private void OnFacebookButtonClicked () {
        if (!FB.IsLoggedIn) {
            FB.LogInWithReadPermissions(new List<string>() { "public_profile", "email", "user_friends", "user_likes" }, LoginCallback);
        }
    }

    void LoginCallback(ILoginResult result)
    {
        if (null == HangarController.Instance) {
            return;
        }
        if (result.Error != null) {
            Debug.LogError ("Facebook login error: " + result.Error);
            ShowLoginButton ();
        }
        else if (!FB.IsLoggedIn) {
            DisableAutologin ();
            ShowLoginButton ();

            #region Google Analytics: Facebook integration cancelled

            GoogleAnalyticsWrapper.LogEvent (
                new CustomEventHitBuilder ()
                    .SetParameter (GAEvent.Category.FacebookIntegration)
                    .SetParameter (GAEvent.Action.Cancelled)
                    .SetParameter (GAEvent.Label.PlayerLevel)
                    .SetValue (ProfileInfo.Level));

#endregion
        }
        else {
            if (!IsAutologinUse ()) {
                EnableAutologin ();
            }

            OnLoggedIn ();

            #region Google Analytics: Facebook integration success

            GoogleAnalyticsWrapper.LogEvent (
                new CustomEventHitBuilder ()
                    .SetParameter (GAEvent.Category.FacebookIntegration)
                    .SetParameter (GAEvent.Action.Succeed)
                    .SetParameter (GAEvent.Label.PlayerLevel)
                    .SetValue (ProfileInfo.Level));

#endregion
        }
    }

    void OnLoggedIn () {
        HideLoginButton ();
#if !(UNITY_WEBPLAYER || UNITY_WEBGL)
        if (IsFirstLogin ()) {
            ActivateFacebook ();
        }
        if (ProfileInfo.fbUserId != UserId) {
            ProfileInfo.fbUserId = UserId;
            ProfileInfo.SaveToServer ();
        }
#else
        ProfileInfo.fbUserId = UserId;
#endif
        //string data = GameData.facebookScores;
        //if (!string.IsNullOrEmpty (data)) {
        //    OnInfoLoaded (new FBResult (data));
        //}
        FB.API (s_loadMainInfo, HttpMethod.GET, OnInfoLoaded);
#if UNITY_ANDROID || UNITY_IPHONE || UNITY_WSA
        CheckPermissions();
#endif
    }

    void OnInfoLoaded (IGraphResult result) {
        if (null == HangarController.Instance) {
            return;
        }
        if (result.Error != null) {
            if (m_scoresReceiveRetries > scoresReceiveRetries) {
                return;
            }
            Debug.LogWarning ("Main info load error: " + result.Error);
            FB.API (s_loadMainInfo, HttpMethod.GET, OnInfoLoaded);
            m_scoresReceiveRetries++;
            return;
        }
        GameData.facebookScores = result.RawResult;
        fbFriendsObjects = Util.DeserializeScores (result.RawResult);
        Dispatcher.Send(EventId.AfterFacebookMainInfoLoaded, null);
        SendScores();
    }

    //public List<object> GetFbFriendsObjects()
    //{
    //    return new List<object>(fbFriendsObjects);
    //}

    public List<string> GetFriendsAppUsersIds()
    {
        List<string> friends_uids = new List<string>();

        foreach (Dictionary<string, object> friend in fbFriendsObjects)
        {
            var prefs = new JsonPrefs(friend);
            friends_uids.Add(prefs.ValueString("user/id"));
        }

        return friends_uids;
    }

    void HideLoginButton () {
        if (null != loginButton) {
            loginButton.gameObject.SetActive (false);
        }
    }

    void ShowLoginButton () {
        if (null != loginButton) {
            loginButton.gameObject.SetActive (true);
        }
    }



    private bool IsFirstLogin () {
        return !ProfileInfo.isSocialActivated;
    }

    private bool IsAutologinUse () {
        return PlayerPrefs.GetInt ("facebookAutologin", 0) != 0;
    }

    private void DisableAutologin () {
        PlayerPrefs.SetInt ("facebookAutologin", 0);
        PlayerPrefs.Save ();
    }

    private void EnableAutologin () {
        PlayerPrefs.SetInt ("facebookAutologin", 1);
        PlayerPrefs.Save ();
    }

    private void ActivateFacebook () {
        PlayerPrefs.SetInt ("facebookAutologin", 1);
        PlayerPrefs.Save ();
    }


    //------------------------------------------------------------------------------------------------------------------
    // Callbacks
    private void FriendsInviteCallback (IAppRequestResult result) {
        if (!string.IsNullOrEmpty (result.Error)) {
            Debug.LogWarning ("Error while friends invite: "+result.Error);
            return;
        }
        var res = result.ResultDictionary;
        if (!res.ContainsKey ("to")) {
            Debug.Log ("Key 'to' not found in answer");
            return;
        }

        List<object> list;
        var to = res["to"];
        if (to is string) {
            list = (to as string).Split(',').Select(x => (object)x).ToList();
        }
        else {
            list = res["to"] as List<object>;
        }

        if ( (list == null) || (list.Count < 1) ) {
            Debug.Log ("No one friend invited, cancel");
            return;
        }

        InviteSuccess();
    }

    private void AppInviteCallback(IAppInviteResult result)
    {
        if (!string.IsNullOrEmpty(result.Error)) {
            Debug.LogWarning("Error while friends invite: " + result.Error);
            return;
        }

        var data = new JsonPrefs(result.ResultDictionary);
        if (data.ValueBool ("cancelled", false)) {
            Debug.Log ("Friends invire cancelled");
        }

        if (!data.ValueBool("didComplete", false)) {
            return;
        }

        InviteSuccess();
    }

    private void InviteSuccess ()
    {
        if (IsBonusForInviteAvailable)
        {
            Http.Manager.FuelForInvite((fResult, response) => {
                Debug.Log("Fuel inreased by " + GameData.fuelForInvite + " for friends invite");
                if (fResult)
                {
                    #region Google Analytics: fuel got via social invitation

                    GoogleAnalyticsWrapper.LogEvent(
                        new CustomEventHitBuilder()
                            .SetParameter(GAEvent.Category.FuelBuying)
                            .SetParameter(GAEvent.Action.GotViaFacebookInvitation)
                            .SetParameter<GAEvent.Label>()
                            .SetSubject(GAEvent.Subject.PlayerLevel, ProfileInfo.Level)
                            .SetValue(ProfileInfo.Gold));

                    #endregion

                }
            });
        }
    }




    #region Send scores to Facebook
    void SendScores ()
    {
        if (AccessToken.CurrentAccessToken == null) {
            return;
        }

        if (!AccessToken.CurrentAccessToken.Permissions.Contains("publish_actions"))
        {
            Debug.Log("Facebook: publish_actions permission required");
            FB.LogInWithPublishPermissions(
                new List<string>() { "publish_actions" },
                delegate (ILoginResult result) {
                    if (!string.IsNullOrEmpty (result.Error))
                    {
                        Debug.LogError("Facebook: login with write permissions failed: " + result.Error);
                        return;
                    }
                    SendScores_DoIt();
                }
            );
            return;
        }

        SendScores_DoIt();
    }

    void SendScores_DoIt ()
    {
        FB.API("/me/scores", HttpMethod.POST,
            delegate (IGraphResult result) {
                if (!string.IsNullOrEmpty(result.Error))
                {
                    Debug.LogError("Facebook send scores failed: " + result.Error);
                    return;
                }
            },
            new Dictionary<string, string>() {
                { "score", ProfileInfo.Experience.ToString (System.Globalization.CultureInfo.InvariantCulture) }
            }
        );
    }
    #endregion



    #region Check permissions

    /// <summary>
    /// Проверяем разрешения у текущего токена на их отзыв игроком и перелогиниваемся в случае если какое-то из разрешений отозвано
    /// </summary>
    void CheckPermissions ()
    {
        FB.API("/me/permissions", HttpMethod.GET,
            delegate (IGraphResult result) {
                if (!string.IsNullOrEmpty(result.Error))
                {
                    Debug.LogError("Facebook: Get permissions failed: " + result.Error);
                    var pr = new JsonPrefs(result.ResultDictionary);
                    if (pr.ValueString ("error/type", "") == "OAuthException") {
                        Debug.LogWarning("Facebook: API request error. Logout.");
                        FB.LogOut();
                        ShowLoginButton();
                    }
                    return;
                }
                if (!result.ResultDictionary.ContainsKey ("data"))
                {
                    return;
                }

                var lst = result.ResultDictionary["data"] as List<object>;
                if (lst == null) {
                    return;
                }

                var perms = new List<string> ();
                foreach (var d in lst) {
                    var pr = new JsonPrefs(d);
                    if (pr.ValueString("status", "") != "granted") {
                        continue;
                    }

                    var p = pr.ValueString("permission", "");
                    if (string.IsNullOrEmpty(p)) {
                        continue;
                    }

                    perms.Add(p);
                }
                var toCheck = new List<string>() { "user_friends" };
                if (AccessToken.CurrentAccessToken.Permissions.Contains("publish_actions"))
                {
                    toCheck.Add("publish_actions");
                }
                foreach (var p in toCheck)
                {
                    if (!perms.Contains (p))
                    {
                        Debug.LogWarning("Facebook: Some permissins ("+p+") was revoked. Logout.");
                        FB.LogOut();
                        ShowLoginButton();
                        return;
                    }
                }

            }
        );

    }

    #endregion
}
