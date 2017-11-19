using System.Collections;
using Tanks.Models;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Vkontakte;
using System.Linq;

class VkMobile : MonoBehaviour,ISocialService
{
    #region ISocialService implementation
    public event Action<ISocialService, SocialInitializationState> InitializationFinished = delegate { };
    public event Action<ISocialService> LoginSucceed = delegate { };
    public event Action LoginFail = delegate { };
    public event Action LogoutSucceed = delegate { };
    public event Action<string> GroupJoined = delegate { };
    public event Action GroupJoinErrorOccured = delegate { };

    VkUser me;
    List<VkUser> friends_app_users = new List<VkUser>();
    static List<VkUser> friends_to_invite = new List<VkUser>();
    
    public static List<VkUser> FriendsToInvite { get { return friends_to_invite; } }
    public void Login()
    {
        VKSdk.login();
    }
    public void Logout()
    {
        VKSdk.logout();
        LogoutSucceed();
    }
    public string Uid()
    {
        return me.Id;
    }
    public string AvatarUrl()
    {
        return me.AvatarUrl;
    }
    public void InviteFriend()
    {
        FriendInviteListController.Instance.Fill();
        GUIPager.SetActivePage("VkFriendInvitation", true);
    }
    public void ShowPayment(string itemId) { }
    public Dictionary<string, string> GetAuthParams()
    {
        return new Dictionary<string, string>
        {
            {"vkontakte",Uid()}
        };
    }
    public SocialPrice GetPriceById(string itemId)
    {
        return null;
    }
    public string GetPriceStringById(string itemId)
    {
        return Localizer.GetText(itemId);
    }
    public void PostNewLevelToWall()
    {
        try
        {
            var text = Localizer.GetText("textNewLevel", ProfileInfo.Level);
            var photoId = GameData.vkImagesLevels[ProfileInfo.Level - 1];
                //VkSettings.LevelImgIds[ProfileInfo.Level - 1];
            ShowShareDialog(text, (string)photoId);
        }
        catch (Exception exception)
        {
            Debug.Log(exception.Message);
        }
    }
    void ShowShareDialog(string text, Texture2D img)
    {
        var attachmentText = GameData.ClearGameFlags(GameData.CurrentGame).ToString();
        var attachmentLink = string.Format("https://vk.com/app{0}", VkSettings.MobileAppId);
        VKSdk.ShowShareDialog(text, img, attachmentText, attachmentLink);
    }
    void ShowShareDialog(string text, string photoId)
    {
        var attachmentText = GameData.ClearGameFlags(GameData.CurrentGame).ToString();
        var attachmentLink = string.Format("https://vk.com/app{0}", VkSettings.MobileAppId);
        VKSdk.ShowShareDialog(text, photoId, attachmentText, attachmentLink);
    }
    public void Post(string text, Texture2D img)
    {        
        ShowShareDialog(text, img);
    }
    public void PostAchievement(AchievementsIds.Id webAchievement, string text)
    {
        try
        {
            var imageId = GameData.vkImagesAchievements[webAchievement.ToString()];
                //VkSettings.AchievementImgIds[webAchievement];
            ShowShareDialog(text, Convert.ToString(imageId));
        }
        catch (Exception exception)
        {
            Debug.Log(exception.Message);
        }
    }
    public List<string> GetFriendsAppUsersIds()
    {
        return friends_app_users.Select(UserVehicle => UserVehicle.Id).ToList();
    }

    public void UpdateSocialActivity()
    {
        bool joined = ProfileInfo.socialActivity.Contains(SocialAction.joined);
        if (!joined)
        {
            var parameters = new Dictionary<string, string>{
                {"group_ids", VkSettings.GameGroupId}
            };
            Action<string> onComplete = delegate(string jsonResult)
            {
                Debug.LogError("UpdateSocialActivity" + jsonResult);
                var isMember = new JsonPrefs(jsonResult).ValueBool("response/0/is_member");
                if (!isMember) return;
                SocialSettings.Instance.ReportSocialActivity(SocialAction.joined);
            };
            StartCoroutine(VkApi.groups().getById(parameters).Start(onComplete));
        }
    }
    public void JoinGroup(string id)
    {
        var parameters = new Dictionary<string,string>
        {
            {"group_id", id}
        };
        Action<string> onComplete = delegate(string jsonResult) {
            //Debug.Log("JoinGroup " + jsonResult);
            bool isMember = new JsonPrefs(jsonResult).ValueBool("response");
            if (isMember)
                GroupJoined(id);
            else
                GroupJoinErrorOccured();
        };
        StartCoroutine(VkApi.groups().join(parameters).Start(onComplete));
    }
    public bool IsLoggedIn {
        get { return VKSdk.isLoggedIn();
        }
    }
    public bool IsInitialized { get { return VKSdk.isInitialized(); } }
    public SocialPlatform Platform { get { return SocialPlatform.Vkontakte; } }
    public Player Player { get; private set; }
    public IEnumerable<SocialNetworkGroup> AllSocialGroups { get; private set; }
    #endregion

    void Start()
    {
        SubscribeToSdkEvents();
        VKSdk.initialize(VkSettings.MobileAppId);
		#if !UNITY_IOS
        if(IsLoggedIn)
        {
            StartCoroutine(Init());
        }
		#endif
    }

    private void SubscribeToSdkEvents()
    {
        VKSdk.InitializationFinished  += delegate{ /*Debug.Log("VKMOBILE InitializationFinished");*/ InitializationFinished(this, SocialInitializationState.Success);};
        VKSdk.LoginSucceed += delegate { /*Debug.Log("VKMOBILE loginsucceed");*/ StartCoroutine(Init()); };
        VKSdk.LoginFail += delegate { /*Debug.Log("VKMOBILE loginfail");*/LoginFail(); };
        VKSdk.LogoutSucceed += delegate { /*Debug.Log("VKMOBILE LogoutSucceed");*/ LogoutSucceed(); };
    }

    IEnumerator LoadMyInfo ()
    {
#if !UNITY_EDITOR
        var parameters = new Dictionary<string, string>{
            {"fields", "photo_50,city,verified"}
        };
        Action<string> onComplete = delegate(string jsonResult)
        {
            //Debug.LogError(jsonResult);
            var prefs = new JsonPrefs(jsonResult);
            if(prefs.Contains("response"))
            {
                string id = prefs.ValueString("response/0/uid", "");
                string avatarUrl = prefs.ValueString("response/0/photo_50", "");
                string firstName = prefs.ValueString("response/0/first_name", "");
                string lastName = prefs.ValueString("response/0/last_name", "");
                me = new VkUser(id, firstName, lastName, avatarUrl);
            }
        };
        yield return StartCoroutine(VkApi.users().get(parameters).Start(onComplete));
#else
        me = new VkUser("271527343", "Sdfgsdfgsdfg", "Sfdgsfdgssdfg", @"http://cs628518.vk.me/v628518637/3fe09/F9H2N-ko_J4.jpg");
        yield break;
#endif
    }

    IEnumerator GetFriendsAppUsers ()
    {
#if !UNITY_EDITOR
        var listOfFriendsUids = new List<string>();
        Action<string> onGetFriendsUidsComplete = delegate(string jsonResult)
        {
            //Debug.LogError(jsonResult);
            var result = new JsonPrefs(jsonResult);
            if (result.Contains("response"))
            {
                var list = result.ValueObjectList("response");
                foreach (var o in list)
                {
                    listOfFriendsUids.Add(o.ToString());
                }

            }

        };
        Action<string> onGetFriendsInfoComplete = delegate(string jsonResult1)
        {
            //Debug.LogError(jsonResult1);
            var vkData = new JsonPrefs(jsonResult1);
            var count = vkData.ValueObjectList("response").Count;
            vkData.BeginGroup("response");
            for (int i = 0; i < count; i++)
            {
                vkData.BeginGroup(i.ToString());
                friends_app_users.Add(new VkUser(vkData.ValueString("uid"),
                                                 vkData.ValueString("first_name"),
                                                 vkData.ValueString("last_name"),
                                                 vkData.ValueString("photo_50")));
                vkData.EndGroup();
            }
            vkData.EndGroup();
            friends_app_users.Add(me);
            
        };
        yield return StartCoroutine(VkApi.friends().getAppUsers(new Dictionary<string, string>()).Start(onGetFriendsUidsComplete));  
        if (listOfFriendsUids.Count > 0) {
            var getFriendsInfoParameters = new Dictionary<string, string>{
                {"user_ids", string.Join(",", listOfFriendsUids.ToArray())},
                {"fields", "photo_50, last_name, first_name"}
            };
            yield return StartCoroutine (VkApi.users().get(getFriendsInfoParameters).Start(onGetFriendsInfoComplete));

        }
#else
        friends_app_users.Add(new VkUser("8326838", "gfdh", "gdfghdfgh", @"http://cs628518.vk.me/v628518637/3fe09/F9H2N-ko_J4.jpg"));
        yield break;
#endif
    }

    IEnumerator GetFriendForInviteList()
    {
#if !UNITY_EDITOR
        var parameters = new Dictionary<string, string>{
            {"fields", "photo_50"},
            {"extended", "1"},
            {"count", "500"},
            {"type","invite"}
        };
        Action<string> onComplete = delegate(string jsonResult)
        {
            //Debug.LogError(jsonResult);
            var result = new JsonPrefs(jsonResult);
            var count = result.ValueInt("response/0");
            result.BeginGroup("response");
            for(int i = 1;i < count+1;i++)
            {
                result.BeginGroup(i.ToString());
                friends_to_invite.Add(new VkUser(result.ValueString("uid"),
                                                 result.ValueString("first_name"),
                                                 result.ValueString("last_name"),
                                                 result.ValueString("photo_50")));
                result.EndGroup();
            }
            result.EndGroup();
        };
        yield return StartCoroutine(VkApi.apps().getFriendsList(parameters).Start(onComplete));
#else
        #region test
        string text = @"{
	""response"" : {
		""count"" : 4,
		""items"" : [{
				""id"" : 47456745,
				""first_name"" : ""Bofgdlfsgjsd"",
				""last_name"" : ""Evsetoifdsn"",
				""photo_50"" : ""http://cs628518.vk.me/v628518637/3fe09/F9H2N-ko_J4.jpg""
			}, {
				""id"" : 234234654,
				""first_name"" : ""Rdlhgkldfsg"",
				""last_name"" : ""Nfdgsdgagsdf"",
				""photo_50"" : ""http://cs625320.vk.me/v625320032/19efa/iiriX0zCf6U.jpg""
			}, {
				""id"" : 154453475,
				""first_name"" : ""Dmadfoads"",
				""last_name"" : ""Zagodfsng"",
				""photo_50"" : ""http://cs625320.vk.me/v625320032/19efa/iiriX0zCf6U.jpg""
			}, {
				""id"" : 5634563,
				""first_name"" : ""Alewrogdfl"",
				""last_name"" : ""Lewrgdskfjg"",
				""photo_50"" : ""http://cs628518.vk.me/v628518637/3fe09/F9H2N-ko_J4.jpg""
			}
		]
	}
}";
        var result = new JsonPrefs(text);
        var count = result.ValueInt("response/count");
        result.BeginGroup("response/items");
        for (int j=0;j<4;j++)
        {
            for (int i = 0; i < count; i++)
            {
                result.BeginGroup(i.ToString());
                friends_to_invite.Add(new VkUser(result.ValueString("id"),
                    result.ValueString("first_name"),
                    result.ValueString("last_name"),
                    result.ValueString("photo_50")));
                result.EndGroup();
            }
        }
            result.EndGroup();
        yield return null;
    #endregion
#endif
    }

    IEnumerator GetGroupsInfo ()
    {
#if !UNITY_EDITOR
        var parameters = new Dictionary<string, string>{
            {"group_ids", VkSettings.GameGroupId+","+VkSettings.VkGamingGroupId}
        };
        var list = new List<SocialNetworkGroup>();
        Action<string> onComplete = delegate(string jsonResult)
        {
            //Debug.LogError(jsonResult);
            var groupsObj = new JsonPrefs(jsonResult).ValueObjectList("response");
            foreach (var current in groupsObj)
            {
                var prefs = new JsonPrefs(current);
                var info = new SocialNetworkGroupInfo(
                    SocialPlatform.Vkontakte,
                    prefs.ValueString("gid"), 
                    prefs.ValueString("name"),
                    "https://vk.com/" + prefs.ValueString("screen_name")); //TODO localized group name
                var group = new SocialNetworkGroup(info, prefs.ValueBool("is_member"), this);
                list.Add(group);
            }
        };
        yield return StartCoroutine(VkApi.groups().getById(parameters).Start(onComplete));      
        AllSocialGroups = list;
#else
        #region test
        var list = new List<SocialNetworkGroup>();
        var info = new SocialNetworkGroupInfo(
            SocialPlatform.Vkontakte,
            "68191181",
            "Iron Tanks",
            "https://vk.com/irontanks");

        var group = new SocialNetworkGroup(info, true, this); 
        list.Add(group);
        info = new SocialNetworkGroupInfo(
            SocialPlatform.Vkontakte,
            "78616012",
            "Игры ВКонтакте",
            "https://vk.com/vkgames");
        group = new SocialNetworkGroup(info, true, this);
        list.Add(group);
        AllSocialGroups = list;
        yield break;
        #endregion
#endif
    }

    IEnumerator Init()
    {
        yield return StartCoroutine(LoadMyInfo());
        yield return StartCoroutine(GetFriendsAppUsers());
        yield return StartCoroutine(GetFriendForInviteList());
        yield return StartCoroutine(GetGroupsInfo());
        Player = new Player(SocialPlatform.Vkontakte, me.Id, me.FirstName, me.LastName, me.AvatarUrl);
        LoginSucceed(this);
    }

}
