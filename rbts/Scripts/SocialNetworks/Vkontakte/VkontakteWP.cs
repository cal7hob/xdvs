using System;
using UnityEngine;
using System.Collections.Generic;
#if UNITY_WP_8_1 && !UNITY_EDITOR
using VK.WindowsPhone.SDK;
#endif

namespace Vkontakte
{
#if UNITY_WP_8_1 && !UNITY_EDITOR
    class VkontakteWP : VkontakteBase
    {
        private bool isInitialized;

        public VkontakteWP()
        {
            VKSDK.AccessTokenReceived += AccessTokenReceived;
            VKSDK.AccessDenied += AccessDenied;
        }

        private void AccessTokenReceived(object sender, VKAccessTokenReceivedEventArgs args)
        {
            //Debug.Log("AccessTokenReceived");
            UnityEngine.WSA.Application.InvokeOnAppThread(() =>
            {
                RaiseLoginSucceedEvent();
            }, false);
        }

        private void AccessDenied(object sender, VKAccessDeniedEventArgs args)
        {
            //Debug.Log("AccessDenied");
            UnityEngine.WSA.Application.InvokeOnAppThread(() =>
            {
                RaiseLoginFailEvent();
            }, false);
        }

        public override string GetAccessToken()
        {
            //Debug.Log("GetAccessToken " + VKSDK.GetAccessToken().AccessToken);
            return VKSDK.GetAccessToken().AccessToken;
        }

        public override string GetUid()
        {
            return VKSDK.GetAccessToken().UserId;
        }

        public override void Login()
        {
            UnityEngine.WSA.Application.InvokeOnUIThread(() =>
            {
                var scopes = new List<string> {VKScope.NOTIFICATIONS,VKScope.FRIENDS,VKScope.PHOTOS,VKScope.WALL,VKScope.GROUPS,VKScope.OFFLINE};
                VKSDK.Authorize(scopes, true, false, LoginType.VKApp);
            }, false);
        }

        public override void Logout()
        {
            VKSDK.Logout();
        }

        public override void Initialize(string appId)
        {
            VKSDK.Initialize(VkSettings.MobileAppId);
            isInitialized = true;
            VKSDK.WakeUpSession(); 
            RaiseInitializationFinishedEvent();
        }

        public override bool IsInitialized()
        {
            return isInitialized;
        }

        public override bool IsLoggedIn()
        {
            //Debug.Log("VKSDK.IsLoggedIn " + VKSDK.IsLoggedIn);
            return VKSDK.IsLoggedIn;
        }

        public override void ShowShareDialog(string text, Texture2D img, string attachmentText, string attachmentLink)
        {
            MessageBox.Show(MessageBox.Type.Info,Localizer.GetText("Excuses"));
        }

        public override void ShowShareDialog(string text, string photoId, string attachmentText, string attachmentLink)
        {
            MessageBox.Show(MessageBox.Type.Info, Localizer.GetText("Excuses"));
        }
    }
#endif
}
