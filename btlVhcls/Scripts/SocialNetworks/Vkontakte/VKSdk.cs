using System;
using UnityEngine;

namespace Vkontakte
{
    public class VKSdk
    {
        private Action initializationFinished;

        public static event Action InitializationFinished
        {
            add { Instance.initializationFinished += value; }
            remove { Instance.initializationFinished -= value; }
        }

        private Action loginSucceed;

        public static event Action LoginSucceed
        {
            add { Instance.loginSucceed += value; }
            remove { Instance.loginSucceed -= value; }
        }

        private Action loginFail;

        public static event Action LoginFail
        {
            add { Instance.loginFail += value; }
            remove { Instance.loginFail -= value; }
        }

        private Action logoutSucceed;

        public static event Action LogoutSucceed
        {
            add { Instance.logoutSucceed += value; }
            remove { Instance.logoutSucceed -= value; }
        }

        private static VKSdk instance;
        private VkontakteBase VkontakteImpl;

        private static VKSdk Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new VKSdk();
                }
                return instance;
            }
        }

        private VKSdk()
        {
            VkontakteImpl =
#if UNITY_ANDROID && !UNITY_EDITOR
            new VkontakteAndroid()
#elif UNITY_IPHONE && !UNITY_EDITOR
            new VkontakteIOS()
#elif UNITY_WP_8_1 && !UNITY_EDITOR
            new VkontakteWP()
#elif UNITY_EDITOR
            new VkontakteEditor()
#else
            new VkontakteEditor()
#endif
                ;
            VkontakteImpl.InitializationFinished +=
                delegate { if (initializationFinished != null) initializationFinished(); };
            VkontakteImpl.LoginSucceed += delegate { if (loginSucceed != null) loginSucceed(); };
            VkontakteImpl.LogoutSucceed += delegate { if (logoutSucceed != null) logoutSucceed(); };
            VkontakteImpl.LoginFail += delegate { if (loginFail != null) loginFail(); };
        }

        public static string getAccessToken()
        {
            return Instance.VkontakteImpl.GetAccessToken();
        }
        public static string getUserId()
        {
            return Instance.VkontakteImpl.GetUid();
        }
        public static void login()
        {
            Instance.VkontakteImpl.Login();
        }

        public static void initialize(string appId)
        {
            Instance.VkontakteImpl.Initialize(appId);
        }

        public static bool isInitialized()
        {
            return Instance.VkontakteImpl.IsInitialized();
        }

        public static bool isLoggedIn()
        {
            return Instance.VkontakteImpl.IsLoggedIn();
        }

        public static void logout()
        {
            Instance.VkontakteImpl.Logout();
        }

        public static void ShowShareDialog(string text, Texture2D img, string attachmentText,
            string attachmentLink)
        {
            Instance.VkontakteImpl.ShowShareDialog(text, img, attachmentText, attachmentLink);
        }

        public static void ShowShareDialog(string text, string photoId, string attachmentText,
            string attachmentLink)
        {
            Instance.VkontakteImpl.ShowShareDialog(text, photoId, attachmentText, attachmentLink);
        }

        /*public static void CheckUserInstall(VkCheckUserInstallListener listener)
    {
        SdkWrapper.CallStatic("CheckUserInstall", listener);
    }*/
    }

    public class Scope
    {
        public const string NOTIFY = "notify";
        public const string FRIENDS = "friends";
        public const string PHOTOS = "photos";
        public const string AUDIO = "audio";
        public const string VIDEO = "video";
        public const string DOCS = "docs";
        public const string NOTES = "notes";
        public const string PAGES = "pages";
        public const string STATUS = "status";
        public const string WALL = "wall";
        public const string GROUPS = "groups";
        public const string MESSAGES = "messages";
        public const string NOTIFICATIONS = "notifications";
        public const string STATS = "stats";
        public const string ADS = "ads";
        public const string OFFLINE = "offline";
        public const string NOHTTPS = "nohttps";
        public const string DIRECT = "direct";
    }

}