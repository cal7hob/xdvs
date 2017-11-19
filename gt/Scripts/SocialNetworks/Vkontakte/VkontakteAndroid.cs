using System;
using UnityEngine;
#if UNITY_ANDROID
namespace Vkontakte
{
    class VkontakteAndroid : VkontakteBase
    {
        private AndroidJavaClass sdk;
        private AndroidJavaClass sdkWrapper;

        public VkontakteAndroid()
        {
            sdkWrapper = new AndroidJavaClass("com.xdevs.vk.sdk.VKSDKWrapper");
            sdk = new AndroidJavaClass("com.vk.sdk.VKSdk");
        }
        public override string GetAccessToken()
        {
            return sdkWrapper.CallStatic<AndroidJavaObject>("getAccessToken").Get<string>("accessToken");
        }

        public override string GetUid()
        {
            return sdkWrapper.CallStatic<string>("getUid");
        }

        public override void Login()
        {
            var listener = new VKListener
            {
                OnResult = () => CallbackProcessor.QueueCallback(RaiseLoginSucceedEvent),
                OnAccessDenied = () => CallbackProcessor.QueueCallback(RaiseLoginFailEvent)
            };
            sdkWrapper.CallStatic("login", listener, new[] { Scope.NOTIFICATIONS, Scope.FRIENDS, Scope.PHOTOS, Scope.WALL, Scope.GROUPS, Scope.OFFLINE });
        }

        public override void Logout()
        {
            sdk.CallStatic("logout");
            RaiseLogoutSucceedEvent();
        }

        public override void Initialize(string appId)
        {
            sdkWrapper.CallStatic("initialize", appId);
        }

        public override bool IsInitialized()
        {
            return sdkWrapper.CallStatic<bool>("isInitialized");
        }

        public override bool IsLoggedIn()
        {
            return sdk.CallStatic<bool>("isLoggedIn");
        }

        public override void ShowShareDialog(string text, Texture2D img, string attachmentText, string attachmentLink)
        {
            string b64img = string.Empty;
            if (null != img)
                b64img = Convert.ToBase64String(img.EncodeToPNG()); 
            var listener = new VKShareDialogListener
            {
                OnCancel = delegate {  },
                OnComplete = delegate(int i) {  }
            };
            sdkWrapper.CallStatic("ShowShareDialog", text, b64img, "", attachmentText, attachmentLink, listener);  
        }

        public override void ShowShareDialog(string text, string photoId, string attachmentText, string attachmentLink)
        {
            var listener = new VKShareDialogListener
            {
                OnCancel = delegate { },
                OnComplete = delegate(int i) { }
            };
            sdkWrapper.CallStatic("ShowShareDialog", text, "", photoId, attachmentText, attachmentLink, listener);  
        }
    }
}
#endif
