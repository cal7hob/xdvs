using System;
using UnityEngine;

namespace Vkontakte
{
    public abstract class VkontakteBase
    {
        public event Action InitializationFinished;
        public event Action LoginSucceed;
        public event Action LoginFail;
        public event Action LogoutSucceed;

        protected void RaiseInitializationFinishedEvent()
        {
            if (InitializationFinished != null)
            {
                InitializationFinished();
            }
        }
        protected void RaiseLoginSucceedEvent()
        {
            if (LoginSucceed != null)
            {
                LoginSucceed();
            }
        }
        protected void RaiseLoginFailEvent()
        {
            if (LoginFail != null)
            {
                LoginFail();
            }
        }
        protected void RaiseLogoutSucceedEvent()
        {
            if (LogoutSucceed != null)
            {
                LogoutSucceed();
            }
        }


        public abstract string GetAccessToken();
        public abstract string GetUid();
        public abstract void Login();
        public abstract void Logout();
        public abstract void Initialize(string appId);
        public abstract bool IsInitialized();
        public abstract bool IsLoggedIn();
        public abstract void ShowShareDialog(string text, Texture2D img, string attachmentText, string attachmentLink);
        public abstract void ShowShareDialog(string text, string photoId, string attachmentText, string attachmentLink);
    }
}