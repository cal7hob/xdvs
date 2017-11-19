using UnityEngine;

namespace Vkontakte
{
    class VkontakteEditor : VkontakteBase
    {
        private static bool initialized;
        private static bool loggedin;
        public override string GetAccessToken()
        {
            return "";
        }

        public override string GetUid()
        {
            return "";
        }

        public override void Login()
        {
            loggedin = true;
            RaiseLoginSucceedEvent();
        }

        public override void Logout()
        {
            loggedin = false;
        }

        public override void Initialize(string appId)
        {
            initialized = true;
        }

        public override bool IsInitialized()
        {
            return initialized;
        }

        public override bool IsLoggedIn()
        {
            return loggedin;
        }

        public override void ShowShareDialog(string text, Texture2D img, string attachmentText, string attachmentLink)
        {
        }

        public override void ShowShareDialog(string text, string photoId, string attachmentText, string attachmentLink)
        {
        }
    }
}