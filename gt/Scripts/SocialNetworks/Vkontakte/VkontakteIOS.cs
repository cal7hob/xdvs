using System;
using System.Runtime.InteropServices;
using UnityEngine;
#if UNITY_IOS
namespace Vkontakte
{
    class VkontakteIOS : VkontakteBase
    {
        private bool isInitialized;

        [DllImport("__Internal")]
        private static extern void _Initialize(string appId);
        [DllImport("__Internal")]
        private static extern void _Login();
        [DllImport("__Internal")]
        private static extern void _Logout();
        [DllImport("__Internal")]
        private static extern void _ShowShareDialog(string text, string img, string photoId, string attachmentText, string attachmentLink);
        [DllImport("__Internal")]
        private static extern bool _IsLoggedIn();
        [DllImport("__Internal")]
        private static extern string _GetAccessToken();
        [DllImport("__Internal")]
        private static extern string _GetUid();
        public VkontakteIOS()
        {
            VkMessageHandler.Initialize();
            SubscribeToAuthEvents();
            RaiseInitializationFinishedEvent();
        }

        void SubscribeToAuthEvents()
        {
            VkMessageHandler.Instance.AuthorizationFinished += HandleAuthorizationFinished;
            VkMessageHandler.Instance.AuthorizationFailed += HandleAuthorizationFailed;
            VkMessageHandler.Instance.TokenExpired += HandleTokenExpired;
            VkMessageHandler.Instance.TokenUpdated += HandleTokenUpdated;
			VkMessageHandler.Instance.InitializationFinished += HandleInitializationFinished;
        }
		void HandleInitializationFinished()
		{
			RaiseInitializationFinishedEvent();
		}

        void HandleTokenUpdated()
        {

        }

        void HandleTokenExpired()
        {

        }

        void HandleAuthorizationFailed()
        {
            RaiseLoginFailEvent();
        }

        void HandleAuthorizationFinished(string message)
        {
            var jsonPrefs = new JsonPrefs(message);
            string error = jsonPrefs.ValueString("error", "");
            string token = jsonPrefs.ValueString("token", "");
            if (!string.IsNullOrEmpty(error))
            {
                RaiseLoginFailEvent();
            }
            if (!string.IsNullOrEmpty(token))
            {
                RaiseLoginSucceedEvent();
            }
        }

        public override string GetAccessToken()
        {
            return _GetAccessToken();
        }

        public override string GetUid()
        {
            return _GetUid();
        }

        public override void Login()
        {
            _Login();
        }

        public override void Logout()
        {
            _Logout();
            RaiseLogoutSucceedEvent();
        }

        public override void Initialize(string appId)
        {
            _Initialize(appId.ToString());
            Debug.Log(appId + " appid log XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX");
            isInitialized = true;
        }

        public override bool IsInitialized()
        {
            return isInitialized;
        }

        public override bool IsLoggedIn()
        {
            return _IsLoggedIn();
        }

        public override void ShowShareDialog(string text, Texture2D img, string attachmentText, string attachmentLink)
        {
            string b64img = string.Empty;
            if(null != img)
                b64img = Convert.ToBase64String(img.EncodeToPNG());
            _ShowShareDialog(text, b64img, string.Empty, attachmentText, attachmentLink);
        }

        public override void ShowShareDialog(string text, string photoId, string attachmentText, string attachmentLink)
        {
            _ShowShareDialog(text, string.Empty, photoId, attachmentText, attachmentLink);
        }
    }
}
#endif