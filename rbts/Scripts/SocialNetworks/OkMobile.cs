#if UNITY_ANDROID || UNITY_IOS
using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Odnoklassniki;
using Odnoklassniki.HTTP;
using Odnoklassniki.Util;
using Tanks.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using XDevs;
using Request = Odnoklassniki.HTTP.Request;
using Service = ServiceSettingsKeys.Service;

namespace SocialNetworks
{
    internal class OkMobile : MonoBehaviour, ISocialService
    {
        #region ISocialService implementation

        public event Action<ISocialService, SocialInitializationState> InitializationFinished = delegate { };
        public event Action<ISocialService> LoginSucceed = delegate { };
        public event Action LoginFail = delegate { };
        public event Action LogoutSucceed = delegate { };
#pragma warning disable 0414, 67
        public event Action<string> GroupJoined = delegate { };
        public event Action GroupJoinErrorOccured = delegate { };
#pragma warning restore 0414, 67

        private OKUserInfo me;
        protected bool isLoggedIn;
        private List<string> friendsAppUsers = new List<string>();
        
        public void Login()
        {
            OK.Auth(delegate(bool success)
            {
                if (success)
                    StartCoroutine(Init());
                else
                    LoginFail();
            });
        }

        public void Logout()
        {
            OK.Logout();
            isLoggedIn = false;
            LogoutSucceed();
        }

        public string Uid()
        {
            return me.uid;
        }

        public string AvatarUrl()
        {
            return me.pic1;
        }

        public void InviteFriend()
        {
            if (!hasValuableAccessPermission)
            {
                return;
            }
            var canvas = CreateCanvas();
            tk2dUIManager.Instance.InputEnabled = false;
            OK.OpenInviteDialog(result =>
                {
                    if (SocialSettings.IsBonusForInviteAvailable)
                    {
                        Http.Manager.FuelForInvite((fResult, response) =>
                        {
                            if (fResult)
                            {
                                #region Google Analytics: fuel got via social invitation

                                GoogleAnalyticsWrapper.LogEvent(
                                    new CustomEventHitBuilder()
                                        .SetParameter(GAEvent.Category.FuelBuying)
                                        .SetParameter(GAEvent.Action.GotViaOdnoklassnikiMobileInvitation)
                                        .SetParameter<GAEvent.Label>()
                                        .SetSubject(GAEvent.Subject.PlayerLevel, ProfileInfo.Level)
                                        .SetValue(ProfileInfo.Gold));

                                #endregion
                            }
                        });
                    }

                    tk2dUIManager.Instance.InputEnabled = true;
                    Destroy(canvas.gameObject);
                },
                () =>
                {
                    tk2dUIManager.Instance.InputEnabled = true;
                    Destroy(canvas.gameObject);
                },
                Localizer.GetText("textVkInvitation"));
        }

        public void ShowPayment(string itemId)
        {
        }

        public Dictionary<string, string> GetAuthParams()
        {
            return new Dictionary<string, string>
            {
                {"odnoklassniki", Uid()}
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
                ShowShareDialog(text, "level/" + ProfileInfo.Level);
            }
            catch (Exception exception)
            {
                Debug.Log(exception.Message);
            }
        }

        private void ShowShareDialog(string text, Texture2D img)
        {
            if (!hasValuableAccessPermission)
            {
                //show messagebox 'sooon...'
                return;
            }
            var canvas = CreateCanvas();
            var media = new List<OKMedia>
            {
                OKMedia.Text(text),
                OKMedia.Photo(img)
            };
            tk2dUIManager.Instance.InputEnabled = false;
            OK.OpenPublishDialog(result =>
                {
                    tk2dUIManager.Instance.InputEnabled = true;
                    Destroy(canvas.gameObject);
                },
                () =>
                {
                    tk2dUIManager.Instance.InputEnabled = true;
                    Destroy(canvas.gameObject);
                },
                media);
        }

        private Canvas CreateCanvas()
        {
            var go = new GameObject("Canvas");
            var canvas = go.AddComponent<Canvas>();
            var scaler = go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            go.AddComponent<ScreenManager>().canvasScaler = scaler;
            canvas.pixelPerfect = true;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
            return canvas;
        }

        private void ShowShareDialog(string text, string photoId)
        {
            var canvas = CreateCanvas();
            var link = ServiceSettings.Services[Service.Odnoklassniki][ServiceSettingsKeys.Field.BaseUrlForPostImages] + photoId + ".png";
            var media = new List<OKMedia>
            {
                OKMedia.Text(text),
                OKMedia.App(text, link, photoId, Application.productName, "https://ok.ru/game/"+OKSettings.AppId, " ")
            };
            tk2dUIManager.Instance.InputEnabled = false;
            OK.OpenPublishDialog(result =>
            {
                tk2dUIManager.Instance.InputEnabled = true;
                Destroy(canvas.gameObject);
            }, () =>
            {
                tk2dUIManager.Instance.InputEnabled = true;
                Destroy(canvas.gameObject);
            }, media);
        }

        public void Post(string text, Texture2D img)
        {
            ShowShareDialog(text, img);
        }

        public void PostAchievement(AchievementsIds.Id achievement, string text)
        {
            try
            {
                ShowShareDialog(text, "achievements/" + Convert.ToString(achievement));
            }
            catch (Exception exception)
            {
                Debug.Log(exception.Message);
            }
        }

        public List<string> GetFriendsAppUsersIds()
        {
            return friendsAppUsers;
        }

        public void UpdateSocialActivity()
        {
            bool joined = ProfileInfo.socialActivity.Contains(SocialAction.joined);
            if (!joined)
            {
                var parameters = new Dictionary<string, string>
                {
                    {"group_id", GameData.socialGroups["ok"].ToString()},
                    {"uids", Uid()}
                };
                OK.API("group.getUserGroupsByIds", parameters, delegate(Response result)
                {
                    Debug.Log("group.getUserGroupsByIds response: " + result.message);
                    string[] goodStatuses = {"ACTIVE", "BLOCKED", "MODERATOR", "ADMIN"};
                    string status = new JsonPrefs(result.message).ValueString("data/0/status", "UNKNOWN");
                    bool isMember = goodStatuses.Contains(status);
                    if (isMember)
                    {
                        SocialSettings.Instance.ReportSocialActivity(SocialAction.joined);
                    }
                });
            }
        }

        public void JoinGroup(string id)
        {
            Application.OpenURL(
                string.Format("https://ok.ru/group/{0}",
                    ServiceSettings.Services[Service.Odnoklassniki][ServiceSettingsKeys.Field.GameGroupId]));
        }

        public bool IsLoggedIn
        {
            get { return OK.IsLoggedIn; }
        }

        public bool IsInitialized
        {
            get { return OK.IsInitialized; }
        }

        public SocialNetworkInfo SocialNetworkInfo
        {
            get
            {
                return new SocialNetworkInfo(SocialPlatform.Odnoklassniki, "Одноклассники", "linkPanelok");
            }
        }

        public Player Player { get; private set; }

        public IEnumerable<SocialNetworkGroup> AllSocialGroups
        {
            get { return allSocialGroups; }
        }

        #endregion
        
        private void Awake()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }
   
        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }

        private void Start()
        {
            OK.Init(success => 
            {
                if (success)
                {
                    if (OK.IsLoggedIn)
                    {
                        StartCoroutine(Init());
                    }
                    else
                    {
                        InitializationFinished(this, SocialInitializationState.Success);
                    }
                }
                else
                {
                    InitializationFinished(this, SocialInitializationState.Error);
                }
            });
        }

        private IEnumerator LoadMyInfo()
        {
            bool finish = false;
            OK.GetCurrentUser(info =>
            {
                me = info;
                finish = true;
            });
            yield return new WaitUntil(() => finish);
        }

        private IEnumerator GetFriends()
        {
            bool finish = false;
            OK.API(OKMethod.Friends.get, Method.GET, response =>
            {
                //Debug.Log("OKMethod.Friends.get response.text: " + response.Text);
                //var uidList = new JsonPrefs(response.Text).ValueObjectList("/").ConvertAll(input => input.ToString());
                OK.API(OKMethod.Friends.getAppUsers, Method.GET, result =>
                {
                    var prefs = new JsonPrefs(result.Text);
                    if (prefs.Contains("uids"))
                    {
                        friendsAppUsers = prefs.ValueObjectList("uids").ConvertAll(input => input.ToString());
                    }
                    finish = true;
                });
            });
            yield return new WaitUntil(() => finish);
        }

        private IEnumerator GetGroupsInfo()
        {
            bool finish = false;
            var parameters = new Dictionary<string, string>
            {
                {"uids", ServiceSettings.Services[Service.Odnoklassniki][ServiceSettingsKeys.Field.GameGroupId]},
                {"fields", "uid,name,shortname"}
            };
            var list = new List<SocialNetworkGroup>();
            OK.API("group.getInfo", parameters, delegate(Response result)
            {
                var prefs = new JsonPrefs(result.Text);
                var info = new SocialNetworkGroupInfo(prefs.ValueString("/0/uid"), prefs.ValueString("/0/name"),
                    "linkPanelok", "https://ok.ru/group/" + prefs.ValueString("/0/shortname"));
                parameters = new Dictionary<string, string>
                {
                    {"uids", Uid()},
                    {"group_id", ServiceSettings.Services[Service.Odnoklassniki][ServiceSettingsKeys.Field.GameGroupId]}
                };
                OK.API("group.getUserGroupsByIds", parameters, delegate(Response result1)
                {
                    //Debug.Log("group.getUserGroupsByIds response: " + result1.Text);
                    string[] goodStatuses = {"ACTIVE", "BLOCKED", "MODERATOR", "ADMIN"};
                    string status = new JsonPrefs(result1.Text).ValueString("/0/status", "UNKNOWN");
                    bool isMember = goodStatuses.Contains(status);
                    var group = new SocialNetworkGroup(info, isMember, this);
                    list.Add(group);
                    allSocialGroups = list;
                    finish = true;
                });
            });
            yield return new WaitUntil(() => finish);
        }

        private IEnumerator Init()
        {
            RefreshToken();
            yield return new WaitWhile(() => isTokenRefreshing);
            yield return StartCoroutine(LoadMyInfo());
            yield return StartCoroutine(CheckPermission(OKScope.VALUABLE_ACCESS, b => hasValuableAccessPermission = b));
            yield return StartCoroutine(CheckPermission(OKScope.PHOTO_CONTENT, delegate { }));
            if (hasValuableAccessPermission)
            {
                yield return StartCoroutine(GetFriends());
                yield return StartCoroutine(GetGroupsInfo()); 
            }
            Player = new Player(SocialPlatform.Odnoklassniki, me.uid, me.firstName, me.lastName, me.pic1);
            InitializationFinished(this, SocialInitializationState.Success);
            isLoggedIn = true;

            LoginSucceed(this);
            if (GameData.IsHangarScene)
            {
                IapManager.Instance.OnPurchased += HandlePurchase;
            }
        }

        private void OnActiveSceneChanged(Scene previousScene, Scene newScene)
        {
            if (IsLoggedIn && GameData.IsHangarScene)
            {
                IapManager.Instance.OnPurchased += HandlePurchase;
            }
        }

        private void HandlePurchase(IapPayment payment)
        {
            try
            {
                var receipt = new JsonPrefs(payment.receipt);
                var trxId = receipt.ValueString("json/orderId");
                if (string.IsNullOrEmpty(trxId))
                {
                    var json = new JsonPrefs(receipt.ValueString("json"));
                    trxId = json.ValueString("orderId");
                }
                var args = new Dictionary<string, string>()
                {
                    {"trx_id", trxId},
                    { "amount", payment.product.metadata.localizedPriceString },
                    { "currency", payment.product.metadata.isoCurrencyCode }
                };
                foreach (var arg in args)
                {
                    Debug.Log(arg.Key + " = " + arg.Value);
                }
                OK.API("sdk.reportPayment", args, result =>
                {
                    Debug.Log("sdk.reportPayment response: " + result.Text);
                });
            }
            catch (Exception e)
            {
                Debug.Log("Exception thrown:" + e.Message);
            }
        }
        
        private IEnumerator CheckPermission(string permission, Action<bool> callback)
        {
            bool isCheckRunning = true;
            var parameters = new Dictionary<string, string>
            {
                {"ext_perm", permission}
            };
            OK.API("users.hasAppPermission", parameters, delegate(Response result)
            {
                var prefs = new JsonPrefs(result.Text);
                callback(prefs.ValueBool("/"));
                isCheckRunning = false;
            });
            yield return new WaitWhile(() => isCheckRunning);
        }

        private bool isTokenRefreshing;
        private bool hasValuableAccessPermission;

        private bool hasPhotoContentPermission;

        private IEnumerable<SocialNetworkGroup> allSocialGroups = new List<SocialNetworkGroup>();

        private void RefreshToken()
        {
            if (isTokenRefreshing) return;
            if (OK.IsInitialized && OK.AccessTokenExpiresAt < DateTime.Now)
            {
                //Debug.Log("Token need refresh");
                isTokenRefreshing = true;
                if (OK.IsRefreshTokenValid)
                {
                    //Debug.Log("IsRefreshTokenValid = true");
                    OK.RefreshAccessToken(success =>
                    {
                        //Token refreshed
                        Debug.Log("RefreshAccessToken success:" + success);
                        isTokenRefreshing = false;
                    });
                }
                else
                {
                    //Debug.Log("IsRefreshTokenValid = false");
                    OK.RefreshOAuth(success =>
                    {
                        //Token refreshed
                        Debug.Log("RefreshOAuth success:" + success);
                        isTokenRefreshing = false;
                    });
                }
            }
        }

        public void Update()
        {
            if (OK.IsInitialized && isLoggedIn)
                RefreshToken();
        }

    }
}
#endif
