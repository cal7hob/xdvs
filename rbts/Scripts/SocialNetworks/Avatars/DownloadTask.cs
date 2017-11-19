using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SocialNetworks.Avatars
{
    class DownloadTask : Sheduler.Task
    {
        public SocialPlatform Platform { get { return m_rec.platform; } }
        public string Uid { get { return m_rec.uid; } }
        public int Index { get { return m_rec.index; } }

        RegistryRecord m_rec;
        string m_avatarUrl = null;
        Action<DownloadTask, Texture2D> OnAvatarLoaded;

#pragma warning disable 414
        IAvatarRecord m_avatarRecord; // Для удержания места под аватар пока загрузаем его
#pragma warning restore 414

        public DownloadTask (RegistryRecord record, Action<DownloadTask, Texture2D> callback) : base()
        {
            m_rec = record;
            m_avatarRecord = m_rec.GetAvatarRecord(); // Удерживаем место под аватар занятым на момент загрузки
            OnAvatarLoaded = callback;
        }

        override public IEnumerator Run ()
        {
            yield return GetAvatarUrl ();
            yield return Download ();
        }

        private IEnumerator GetAvatarUrl()
        {
            string url = null;

            switch (Platform)
            {
                case SocialPlatform.Odnoklassniki:
                    url = Http.Manager.CurrentServer + "/billing/ok/avatar/" + Uid;
                    break;
                case SocialPlatform.Mail:
                    url = Http.Manager.CurrentServer + "/billing/mailru/avatar/" + Uid;
                    break;
                case SocialPlatform.Vkontakte:
#if UNITY_WEBGL
                    yield return WebTools.Jsonp ("https://api.vk.com/method/users.get",
                        new Dictionary<string, object> {{"https",1},{"user_ids", Uid },{"fields", "photo_50"}},
                        delegate(string s) { m_avatarUrl = new JsonPrefs(s).ValueString("response/0/photo_50"); }
                    );
#endif
                    url = "https://api.vk.com/method/users.get?https=1&fields=photo_50&user_ids=" + Uid;
                    break;
                case SocialPlatform.Facebook:
                    m_avatarUrl = "https://graph.facebook.com/" + Uid + "/picture?type=square";
                    break;
            }

            if ( string.IsNullOrEmpty (m_avatarUrl) && !string.IsNullOrEmpty (url) )
            {
#if !UNITY_WEBGL
                WWW loader = new WWW(url);

                yield return loader;

                if (loader.error != null)
                    yield break;

                if (Platform == SocialPlatform.Vkontakte) {
                    JsonPrefs dict = new JsonPrefs(loader.text);
                    m_avatarUrl = dict.ValueString("response/0/photo_50", "");
                }
                else {
                    m_avatarUrl = loader.text;
                }
#else
                bool finished = false;
                Callback callback = CallbackPool.instance.getCallback(CallbackType.DISPOSABLE);
                callback.action = (obj, cbk) =>
                {
                    finished = true;
                    if (Platform == SocialPlatform.Vkontakte)
                    {
                        JsonPrefs dict = new JsonPrefs((string)obj);
                        m_avatarUrl = dict.ValueString("response/0/photo_50", "");
                    }
                    else
                    {
                        m_avatarUrl = (string) obj;
                    }
                };
                string eval = @"
                    var XHR = new XMLHttpRequest();
                    XHR.open('POST', 'URL', true);
                    XHR.onreadystatechange = function() {
                            if (XHR.readyState != 4) return;
                            var response = XHR.responseText;
                            container.callback(CALLBACK_ID, response);
                        };
                    XHR.send(null);"
                    .Replace("URL", url)
                    .Replace("CALLBACK_ID", "" + callback.id);
                Application.ExternalEval(eval);
                while(!finished)
                    yield return null;
#endif
            }
        }

        private IEnumerator Download ()
        {
            if (!string.IsNullOrEmpty(m_avatarUrl))
            {
#if !(UNITY_WEBPLAYER || UNITY_WEBGL) || UNITY_EDITOR
                WWW loader = new WWW(m_avatarUrl);

                yield return loader;

                DownloadFinished(loader);
#else
                WebAvatarDownloader.Instance.Download (m_avatarUrl, delegate(object result, Callback callback)
                {
                    DownloadFinished((string)result);
                });
                yield break;
#endif
            }
        }

        private void DownloadFinished(WWW loader)
        {
            if (!string.IsNullOrEmpty(loader.error)) {
                Debug.LogError("Avatar.DownloadTask.DownloadFinished error: " + loader.error);
                OnAvatarLoaded(this, null);
                return;
            }

            OnAvatarLoaded(this, loader.texture);
        }

        private void DownloadFinished(string result_string)
        {
            if (string.IsNullOrEmpty (result_string)) {
                Debug.LogError ("Avatar.DownloadTask.DownloadFinished error");
                OnAvatarLoaded (this, null);
                return;
            }

            Texture2D texture = new Texture2D (2, 2, TextureFormat.ARGB32, false);

            byte[] bytes = Convert.FromBase64String (result_string.Split(',')[1]);

            if (texture.LoadImage (bytes)) {
                OnAvatarLoaded (this, texture);
            }
            else {
                OnAvatarLoaded (this, null);
            }
        }
    }
}
