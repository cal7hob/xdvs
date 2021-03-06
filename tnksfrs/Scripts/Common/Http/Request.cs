using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Linq;
using CodeStage.AntiCheat.ObscuredTypes;
using XD.ExternalPlatforms;
using XD;

namespace Http
{
    public class Request
    {
        public delegate void WWWResultCallback (Response result);

        string m_url;
        WWWForm m_form = new WWWForm ();
        Dictionary<string, string> m_headers = new Dictionary<string, string> ();
        WWW m_caller;
        Response m_response;

        float m_requestStart = 0;
        float m_requestStop = 0;

        public string Url {
            get { return m_url; }
        }

        public WWW Result {
            get { return m_caller; }
        }

        public WWWForm Form {
            get { return m_form; }
        }

        public Dictionary<string, string> Headers {
            get { return m_headers; }
        }

        public Request (string url) {
            m_url = Manager.protocol + "://" + url;
        }

        public WWW CreateWWW ()
        {
            var formDict = new Dictionary<string, string>();
            formDict["application"] = GameData.instance.GetBundleId ();
            formDict["version"] = GameData.instance.GetBundleVersion ();
            formDict["device"] = ProfileInfo.AppGUID.ToString();
            formDict["nickName"] = ProfileInfo.PlayerName;
#if UNITY_ANDROID
            // Интеграция с Google Play Services
            if (GooglePlayGames.PlayGamesPlatform.Instance.IsAuthenticated())
            {
                formDict["googlePlus"] = GooglePlayGames.PlayGamesPlatform.Instance.GetUserId();
            }
#endif
            /*if (StaticType.SocialSettings.Instance<ISocialSettings>() == null)
            {
                Debug.LogError("TF: StaticType.SocialSettings.Instance<ISocialSettings>() == null");
            }
            else if (StaticType.SocialSettings.Instance<ISocialSettings>().GetSocialService() == null)
            {
                Debug.LogError("TF: StaticType.SocialSettings.Instance<ISocialSettings>().GetSocialService() == null");
            }
            else if (StaticType.SocialSettings.Instance<ISocialSettings>().GetSocialService().GetAuthParams() == null)
            {
                Debug.LogError("TF: StaticType.SocialSettings.Instance<ISocialSettings>().GetSocialService().GetAuthParams() == null");
            }
            else
            {
                Dictionary<string, string> dict = StaticType.SocialSettings.Instance<ISocialSettings>().GetSocialService().GetAuthParams();
                foreach (var pair in dict)
                {
                    formDict[pair.Key] = pair.Value;
                }
            }*/


            /*if (PlatformsFactory.lastPlatformActive != null)
            {
                Dictionary<string, string> dict = PlatformsFactory.lastPlatformActive.GetAuthParams();
                foreach (var pair in dict)
                {
                    formDict[pair.Key] = pair.Value;
                }
            }*/

            Dictionary<string, string> dict = StaticType.SocialSettings.Instance<ISocialSettings>().GetSocialService().GetAuthParams();
            foreach (var pair in dict)
            {
                formDict[pair.Key] = pair.Value;
            }
            // PLATFORM
            formDict["platform"] =
#if UNITY_EDITOR
                "android"
#elif UNITY_STANDALONE_OSX
                "macos"
#elif UNITY_STANDALONE
                "pc"
#elif UNITY_ANDROID
                "android"
#elif UNITY_WEBPLAYER
                "webplayer"
#elif UNITY_WEBPLAYER && UNITY_EDITOR
                "android"
#elif UNITY_WP8
                "wp8"
#elif UNITY_IPHONE
                "ios"
#elif UNITY_WSA && UNITY_WP_8_1
                "winrt"
#elif UNITY_WSA && UNITY_WSA_8_1
                "wsa"
#elif UNITY_WSA && UNITY_WSA_10_0
                "uwp"
#elif UNITY_WEBGL
                "webgl"
#endif
;

            var market =
#if UNITY_EDITOR
                "google"
#elif UNITY_ANDROID
                "google"
#elif UNITY_WEBGL
                SocialSettings.Platform.ToString().ToLower()
                //PlatformsFactory.lastPlatformActive.TypePlatform.ToString().ToLower();
#elif UNITY_WP8 || UNITY_WSA
                 "winstore"
#elif UNITY_IPHONE || UNITY_STANDALONE_OSX
                "appstore"
#elif UNITY_STANDALONE_WIN
                "steam"
#else
                ""
#endif
;

            if (!string.IsNullOrEmpty(market))
            {
                formDict["market"] = market;
            }
            // Session token
            if (!string.IsNullOrEmpty (Manager.Instance ().sessionToken)) {
                formDict["token"] = Manager.Instance ().sessionToken;
            }
            formDict["requestNum"] = (++Manager.Instance().requestNum).ToString();
            if (GameData.instance.AuthenticationKey == null) {
                //DT.Log ("GameData.instance.AuthenticationKey == null");
                Manager.dbgWarn("GameData.instance.AuthenticationKey == null");
            }

            foreach (var pair in formDict)
            {
                m_form.AddField(pair.Key, pair.Value);
            }
            m_headers[Manager.signatureHeader] = computeRequestHash(GameData.instance.AuthenticationKey, m_form);
#if (UNITY_WEBPLAYER || UNITY_WEBGL) && !UNITY_EDITOR
            m_headers["Content-Type"] = "application/x-www-form-urlencoded";
#endif

            if (Manager.s_useDebugOutput) {
                Debug.Log("Request ".RichString("color:yellow") + m_url + ":\n" + 
                    Encoding.UTF8.GetString(m_form.data, 0, m_form.data.Length)
                        .Split('&')
                        .Select(s => WWW.UnEscapeURL(s))
                        .Aggregate((s, next) => s+"\n"+next)
                );
            }

            //Manager.dbg ("Create WWW for " + m_url + ", form data: " + Encoding.UTF8.GetString (m_form.data, 0, m_form.data.Length));
            m_caller = new WWW (m_url, m_form.data, m_headers);
            return m_caller;
        }

        public Response GetResponse () {
            if (m_response == null) {
                m_response = new Response (this);
            }
            return m_response;
        }

        public IEnumerator Call (WWWResultCallback successCallback = null, WWWResultCallback failCallback = null) {
            m_requestStart = Time.realtimeSinceStartup;
            yield return CreateWWW ();
            m_requestStop = Time.realtimeSinceStartup;
            Manager.dbg ("WWW wait done for " + m_caller.url + ", completed for " + (m_requestStop - m_requestStart) + " seconds");

            if (!GetResponse().HaveErrors) {
                if (successCallback != null) {
                    successCallback (m_response);
                    yield break;
                }
            }
            else {
                if (failCallback != null) {
                    failCallback (m_response);
                    yield break;
                }
                else {
                    Debug.LogError ("Request for " + m_caller.url + " failed! Error: " + GetResponse().error);
                }
            }
            Manager.dbgWarn ("No callback called for " + m_caller.url);
        }


        string computeRequestHash (string salt, WWWForm form) {
            return Manager.computeHash (salt + Encoding.UTF8.GetString (form.data, 0, form.data.Length));
        }
    }
}
