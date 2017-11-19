using System;
using System.Collections;
using System.Collections.Generic;
using Http;
using MiniJSON;
using Tanks.Models;
#if !(UNITY_WSA || UNITY_WEBGL)
using TapjoyUnity;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CallbackPool))]
public class VKManager : MonoBehaviour, ISocialService
{
    private static VKManager _instance;
    public static VKManager Instance { get { return _instance; } }
    public string AvatarUrl()
    {
        if (MyData.Photo.StartsWith("http://"))
            MyData.Photo.Insert(4, "s");
        return MyData.Photo;
    }
    private CallbackPool callbackPool;
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
            return new SocialNetworkInfo(SocialPlatform.Vkontakte, "��", "");
        }
    }
    public Player Player { get; private set; }

    public void Login()
    {
    }

    public void Logout()
    {
    }

    public string Uid() { return UserId; }
    public void ShowPayment(string item_id) { Application.ExternalCall("BuyMoney", item_id); }
    public void InviteFriend()
    {
        Screen.SetResolution(960, 600, false);
        Application.ExternalCall("ShowInvite");
    }

    public void UpdateSocialActivity() { StartCoroutine(updateSocialActivity()); }
    public void JoinGroup(string id)
    {
    }

    public IEnumerable<SocialNetworkGroup> AllSocialGroups { get { return new List<SocialNetworkGroup>(); } }

    public Dictionary<string, string> GetAuthParams()
    {
        return new Dictionary<string, string>(){
			{"vkontakte",UserId},
			{"api_id",API_id},
			{"signature",AuthKey}
		};
    }

    private Dictionary<String, SocialPrice> Prices = new Dictionary<string, SocialPrice>();

    public SocialPrice GetPriceById(string item_id)
    {
        return Prices[item_id];
    }
    public string UserId { get { return url_vars["viewer_id"]; } }
    public string AuthKey { get { return url_vars["auth_key"]; } }
    public string Protocol { get; private set; }
    public string API_id { get { return url_vars["api_id"]; } }
    public string AccessToken { get { return url_vars["access_token"]; } }

    public static VKData MyData;

    private Dictionary<string, string> url_vars = new Dictionary<string, string>();
    private Dictionary<string, object> app_info = new Dictionary<string, object>();
    private List<string> friends_app_users = new List<string>();

    private void Awake()
    {
        _instance = this;
        callbackPool = CallbackPool.instance;
        callbackPool.initialize();
        Dispatcher.Subscribe(EventId.ServerDataReceived, PopulatePrices);
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }
   
    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void PopulatePrices(EventId eventId, EventInfo eventInfo)
    {
        foreach (var socialPrice in GameData.socialPrices)
        {
            Prices[socialPrice.Key] = new SocialPrice(Convert.ToInt32(socialPrice.Value), SocialCurrency.Vote);
        }
    }

    void Start()
    {
        StartCoroutine(Init());
    }

    private IEnumerator Init()
    {
        yield return StartCoroutine(GetUrlVars());
        yield return StartCoroutine(GetAppInfo());
        yield return StartCoroutine(GetUserInfo(UserId, data => MyData = data));
        yield return StartCoroutine(GetFriendsList());
        LoginSucceed(this);
    }


    private IEnumerator GetFriendsList()
    {
#if !UNITY_WSA
        bool finished = false;
        VKApiCall("friends.getAppUsers", new Dictionary<string, object>(), delegate(object o, Callback callback)
        {
            friends_app_users = new JsonPrefs((string) o).ValueObjectList("response").ConvertAll(input => input.ToString());
            friends_app_users.Add(UserId);
            finished = true;
        });
        while(!finished)
            yield return null;
#else
        yield break;
#endif

    }

    private IEnumerator GetUrlVars()
    {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        bool finished = false;
        Callback callback = callbackPool.getCallback(CallbackType.DISPOSABLE);
        callback.action = delegate(object arg1, Callback arg2)
        {
            string result = (string) arg1;
            if (result.StartsWith("https"))
                Protocol = "https";
            else
                Protocol = "http";

            result = result.Split('?')[1];

            var hashes = result.Split('&');
            foreach (var hash in hashes)
            {
                var url_var = hash.Split('=');
                url_vars[url_var[0]] = url_var[1];
            }
            finished = true;
        };
        string eval = @"container.callback(CALLBACK_ID, document.location.href+'');"
            .Replace("CALLBACK_ID", "" + callback.id);
        Application.ExternalEval(eval);
        while(!finished)
            yield return null;
#else
        yield break;
#endif
    }
    private IEnumerator GetAppInfo()
    {
        bool finished = false;
        VKApiCall("apps.get", new Dictionary<string, object> { { "app_id", API_id } },
            delegate(object o, Callback callback)
            {
                app_info = new JsonPrefs((string)o).ValueObjectDict("response");
                finished = true;
            });
        while(!finished)
            yield return null;
    }
    public IEnumerator GetUserInfo(string viewer_id, Action<VKData> OnComplete)
    {
        bool finished = false;
        var parameters = new Dictionary<string, object>
        {
            {"user_ids", UserId},
            {"fields", "first_name, photo, last_name"}
        };
        VKApiCall("users.get", parameters, delegate(object o, Callback callback)
        {
            var prefs = new JsonPrefs((string)o);
            var socData = new VKData
            {
                ViewerId = prefs.ValueString("response/0/uid"),
                FirstName = prefs.ValueString("response/0/first_name"),
                LastName = prefs.ValueString("response/0/last_name"),
                Photo = prefs.ValueString("response/0/photo"),
            };
            OnComplete(socData);
            finished = true;
        });
        while(!finished)
            yield return null;
    }

    public string GetPriceStringById(string item_id)
    {
        return GetPriceById(item_id).PriceString;
    }

    private void OnActiveSceneChanged(Scene previousScene, Scene newScene)
    {
        if (GameData.IsHangarScene)
        {
            StartCoroutine(GetFriendsList());
        }
    }

    private IEnumerator updateSocialActivity()
    {
        bool joined = ProfileInfo.socialActivity.Contains(SocialAction.joined);
        bool bookmarked = ProfileInfo.socialActivity.Contains(SocialAction.bookmarked);
        bool invited = ProfileInfo.socialActivity.Contains(SocialAction.invited);
        bool shared = ProfileInfo.socialActivity.Contains(SocialAction.shared);
        if(!joined)
            yield return StartCoroutine(CheckGroupMembership(delegate(bool isMember)
            {
                if(isMember)
                {
                    SocialSettings.Instance.ReportSocialActivity(SocialAction.joined);
                    joined = true;
                }
            }));
        if(!bookmarked)
            yield return StartCoroutine(CheckPermissions(256, delegate(bool havePermissions)
            {
                if(havePermissions)
                {
                    SocialSettings.Instance.ReportSocialActivity(SocialAction.bookmarked);
                    bookmarked = true;
                }
            }));

        if (!invited)
            yield return StartCoroutine(GetFriendsAppUsersCount(delegate(int count)
            {
                if(count >= 3)
                {
                    SocialSettings.Instance.ReportSocialActivity(SocialAction.invited);
                    invited = true;
                }
            }));
        var info = new Dictionary<string, object> {{"shared",shared},{"invited", invited},{"joined",joined},{"bookmarked",bookmarked}};
        string eval = string.Format("updateSocialActivity({0});",Json.Serialize(info));
        Application.ExternalEval(eval);
    }

    private IEnumerator GetFriendsAppUsersCount(Action<int> OnComplete)
    {
        bool finished = false;
        VKApiCall("friends.getAppUsers", new Dictionary<string, object>(),
            delegate(object o, Callback callback)
            {
                var friends = new JsonPrefs((string)o).ValueObjectList("response");
                OnComplete(friends.Count);
                finished = true;
            });
        while (!finished)
            yield return null;
    }

    private IEnumerator CheckPermissions(int i, Action<bool> OnComplete)
    {
        bool finished = false;
        VKApiCall("account.getAppPermissions", new Dictionary<string, object>(),
            delegate(object o, Callback callback)
            {
                var permissions = new JsonPrefs((string) o).ValueInt("response");
                OnComplete((permissions & i) == i);
                finished = true;
            });
        while (!finished)
            yield return null;
    }

    public IEnumerator CheckGroupMembership(Action<bool> OnComplete)
    {
        var parameters = new Dictionary<string, object>
        {
            {"user_id", UserId},
            {"group_id", app_info["author_group"]}
        };
        bool finished = false;
        VKApiCall("groups.isMember", parameters,
            delegate(object o, Callback callback)
            {
                bool is_member = new JsonPrefs((string)o).ValueBool("response");
                OnComplete(is_member);
                finished = true;
            });
        while (!finished)
            yield return null;
    }
    public void PostNewLevelToWall()
    {
        try
        {
            var photoId = GameData.vkImagesLevels[ProfileInfo.Level - 1];
                //VkSettings.LevelImgIds[ProfileInfo.Level - 1];
            Application.ExternalCall("PostToWall", ProfileInfo.Level.ToString(), photoId);
        }
        catch (Exception exception)
        {
            Debug.Log(exception.Message);
        }
    }

    public void PostAchievement(AchievementsIds.Id webAchievement, string text)
    {
        if (Screen.fullScreen)
            Screen.SetResolution(960, 600, false);
        try
        {
            var imageId = GameData.vkImagesAchievements[webAchievement.ToString()];
            Application.ExternalCall("PostAchievementToWall", imageId, text + " https://vk.com/app" + API_id);
        }
        catch (Exception exception)
        {
            Debug.Log(exception.Message);
        }
    }

    public void Post(string text, Texture2D img)
    {
        StartCoroutine(PostToWall(text, img));
    }

    private string uploadUrl;
    private string uploadResponse;
    private string photoJson;
    private IEnumerator GetUploadUrl()
    {
        bool finished = false;
        VKApiCall("photos.getWallUploadServer", new Dictionary<string, object> { { "access_token", AccessToken } },
            delegate(object obj, Callback callback)
            {
                uploadUrl = new JsonPrefs((string)obj).ValueString("response/upload_url");
                finished = true;
            });
        while (!finished)
            yield return new WaitForSeconds(0.1f);
    }
    private IEnumerator Upload(Texture2D img)
    {
        Debug.Log(uploadUrl);
        var request = Manager.Instance().CreateRequest("/upload/vk/wall");
        request.Form.AddField("uploadUrl", uploadUrl);
        //byte[] arr = img.EncodeToJPG();
        request.Form.AddField("image", Convert.ToBase64String(img.EncodeToJPG()));
        bool finished = false;
        Manager.StartAsyncRequest(request, delegate(Response response)
        {
            Debug.Log(response.text);
            uploadResponse = response.text;
            finished = true;
        });
        while (!finished)
            yield return new WaitForSeconds(0.1f);
    }
    private IEnumerator CommitUpload()
    {
        bool finished = false;
        var result = new JsonPrefs(uploadResponse);
        var parameters = new Dictionary<string, object>
            {
                {"server", result.ValueString("server")},
                {"photo", result.ValueString("photo")},
                {"hash", result.ValueString("hash")},
                {"access_token", AccessToken}
            };
        VKApiCall("photos.saveWallPhoto", parameters, delegate(object obj, Callback callback)
        {
            photoJson = (string)obj;
            finished = true;
        });
        while (!finished)
            yield return new WaitForSeconds(0.1f);
    }
    private IEnumerator UploadImage(Texture2D img, Action<string> OnSuccess, Action<string> OnFail)
    {
        uploadUrl = uploadResponse = photoJson = string.Empty;
        yield return StartCoroutine(GetUploadUrl());
        if (!string.IsNullOrEmpty(uploadUrl)) yield return StartCoroutine(Upload(img));
        if (!string.IsNullOrEmpty(new JsonPrefs(uploadResponse).ValueString("photo"))) yield return StartCoroutine(CommitUpload());
        if (!string.IsNullOrEmpty(photoJson))
            OnSuccess(photoJson);
        else
            OnFail("Error uploading photo");
    }
    private IEnumerator PostToWall(string text, Texture2D img)
    {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        XdevsSplashScreen.SetActiveWaitingIndicator(true);
        yield return StartCoroutine(UploadImage(img, delegate(string photo_json)
        {
            XdevsSplashScreen.SetActiveWaitingIndicator(false);
            string photo_id = new JsonPrefs(photo_json).ValueString("response/0/id");
            if (!string.IsNullOrEmpty(photo_id))
            {
                if (Screen.fullScreen)
                    Screen.SetResolution(960, 600, false);
                string eval = "(function (){VK.api(\"wall.post\", {message: \"TEXT\",attachments : \"https://vk.com/appAPPID,PHOTOID\"},function(data){console.log(data);});})();"
                    .Replace("TEXT", text + " https://vk.com/appAPPID")
                    .Replace("APPID", API_id)
                    .Replace("PHOTOID", photo_id);
                Application.ExternalEval(eval);
            }
        }, delegate(string error_json)
        {
            XdevsSplashScreen.SetActiveWaitingIndicator(false);
        }));
#else
        yield break;
#endif
    }

    public List<string> GetFriendsAppUsersIds()
    {
        return friends_app_users;
    }

    #region CALLBACKS

    public void GiveMoney(string item_name)
    {

#if !(UNITY_WSA || UNITY_WEBGL)
        if(Prices.ContainsKey(item_name))
            Tapjoy.TrackPurchase(item_name, Prices[item_name].Currency.ToString(), Prices[item_name].Value);
#endif
        XdevsSplashScreen.SetActiveWaitingIndicator(false);
        ProfileInfo.SaveToServer();
    }

    public void OnOrderCancelFail()
    {
        XdevsSplashScreen.SetActiveWaitingIndicator(false);
    }

    private void FriendInviteCallback()
    {
        if (SocialSettings.IsBonusForInviteAvailable)
        {
            Http.Manager.FuelForInvite((fResult, response) => {
                if (fResult)
                { 
                    #region Google Analytics: fuel got via social invitation

                    GoogleAnalyticsWrapper.LogEvent(
                        new CustomEventHitBuilder()
                            .SetParameter(GAEvent.Category.FuelBuying)
                            .SetParameter(GAEvent.Action.GotViaMoimirInvitation)
                            .SetParameter<GAEvent.Label>()
                            .SetSubject(GAEvent.Subject.PlayerLevel, ProfileInfo.Level)
                            .SetValue(ProfileInfo.Gold));

                    #endregion
                    GUIPager.Back();
                }
            });
        }
    }

    #endregion

    public void VKApiCall(string functionName, Dictionary<string, object> parameters, Action<object, Callback> action)
    {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        Callback callback = callbackPool.getCallback(CallbackType.DISPOSABLE);
        callback.action = action;
        string eval = @"
			container._callbackCALLBACK_ID = function (data){
				container.callback(CALLBACK_ID, JSON.stringify(data));
			}
			VK.api('FUNCTION_NAME', PARAMETERS, container._callbackCALLBACK_ID);"
            .Replace("CALLBACK_ID", "" + callback.id)
            .Replace("FUNCTION_NAME", functionName)
            .Replace("PARAMETERS", Json.Serialize(parameters));
        Debug.Log("VKApiCall external evaluation: \n" + eval);
        Application.ExternalEval(eval);
#endif
    }
}

public class VKData
{
    public string ViewerId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Photo { get; set; }

    private string _formatNameCache;

    public string FormatName
    {
        get
        {
            if (string.IsNullOrEmpty(_formatNameCache))
            {
                _formatNameCache = FirstName + " " + LastName;
                if (_formatNameCache.Length > 20)
                {
                    _formatNameCache = _formatNameCache.Remove(17) + "...";
                }
            }

            return _formatNameCache;
        }
    }
}
