using System.Collections.Generic;
using System.Text;
using System;
using UnityEngine;

namespace Http
{
    public enum Error
    {
        NoError = 0,

        // internal
        InternalInvalidParameters = 1000,
        InternalInvalidNickname = 1001,
        InternalServerError = 1002,
        InternalInvalidChatMessage = 1003,
        InternalInvalidFeedbackMessage = 1004,

        // shop
        ShopNotEnoughMoney = 3000,

        // Player
        PlayerBanned = 4000,
        PlayerUnknownToken = 4001,
        PlayerNotInClan = 4002,

        // Clan
        ClanInvalidName = 5000,
        ClanInvalidSlogan = 5001,
        ClanLowLevelForCreateClan = 5002,
        ClanLowLevelForJoinClan = 5003,
        ClanIsFull = 5004,
        ClanNameAlreadyExists = 5005,
        ClanCanNotRemoveCommander = 5006,
        ClanMemberAlreadyInClan = 5007,
        ClanMemberIsNotCommander = 5008,
        ClanPlayerIsNotInClan = 5009
    }

    struct Servers
    {
        public string dev;
        public string real;

        public Servers(string dev, string real)
        {
            this.dev = dev;
            this.real = real;
        }
    }

    class Manager : MonoBehaviour
    {
        public static string signatureHeader = "X-SIGNATURE";

        // Current server
        public string server;
        public bool useDebugOutput = false;

        private const int DEBUG_MESSAGE_MAX_LENGTH = 16300;

        private Dictionary<Interface, Servers> servers = new Dictionary<Interface, Servers>
        {                
            { Interface.Armada2,
                new Servers("tankforce.scifi-tanks.com", "tankforce.extreme-developers.com") },         
        };

        public const string ROUTE_PROFILE = "/public/profile/";//открытие личного кабинета
        public const string ROUTE_RESETPASSWORD = "/public/resetPassword/";
        public const string ROUTE_SUPPORT = "/public/profile/feedback"; // Техподдержка, кнопка btnSupport

        // Debug output on true
        public static bool s_useDebugOutput = false;
        public static string protocol = "https";

        public static string CurrentServer
        {
            get
            {
                return protocol + "://" + Instance().server;
            }
        }

        public string sessionToken = "";
        public int requestNum = 0;

        public BattleServer battleServer;

        public static Manager Instance ()
        {
            if (null == m_instance) {
                GameObject o = new GameObject ();
                o.name = "HttpManager";
                o.transform.position = Vector3.zero;

                o.AddComponent<Manager> ();
            }
            return m_instance;
        }

        private static Manager m_instance;

        private string GetServer()
        {
            return PlayerPrefs.GetString("URL", "tankforce.scifi-tanks.com");
            //return Application.isEditor || Debug.isDebugBuild
            //    ? servers[GameData.CurInterface].dev
            //    : servers[GameData.CurInterface].real;
        }

        void Awake () {
            if (m_instance != null)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad (gameObject);
            m_instance = this;
            battleServer = GetComponent<BattleServer> ();
            if (battleServer == null) {
                battleServer = gameObject.AddComponent<BattleServer> ();
            }

            if (string.IsNullOrEmpty(server))
                server = GetServer();

//#if !(UNITY_WEBPLAYER || UNITY_WEBGL)
            //if (Debug.isDebugBuild)
            {
                protocol = "http";
            }
//#endif
#if UNITY_EDITOR
            s_useDebugOutput = useDebugOutput;
#endif
            //protocol = "http";
            //server = "irontanks.scifi-tanks.com";
            //s_useDebugOutput = true;
        }

#if UNITY_EDITOR
        public void OpenPlayerProfile ()
        {
            var url = protocol + "://" + server + "/admin/player/" + ProfileInfo.playerId;
            Debug.Log ("Open URL: " + url);
            Application.OpenURL (url);
        }
#endif

        public Request CreateRequest(string server, string path)
        {
            if (string.IsNullOrEmpty(server))
            {
                server = this.server;
            }
            return new Request(server + path);
        }

        public Request CreateRequest (string path) {
            return new Request (server + path);
        }

        static public void StartAsyncRequest (Request request, Request.WWWResultCallback successCallback = null, Request.WWWResultCallback failCallback = null) {
            Instance ().StartCoroutine (request.Call (successCallback, failCallback));
        }

        static public string computeHash (string data)
        {
#if UNITY_WP8 || UNITY_WSA
            var hash = new App.Cryptography.SHA512Managed();
#else
            var hash = new System.Security.Cryptography.SHA512Managed ();
#endif
            byte[] ba = hash.ComputeHash (Encoding.UTF8.GetBytes (data));
            var sb = new StringBuilder (ba.Length * 2);
            foreach (byte b in ba) {
                sb.AppendFormat ("{0:x2}", b);
            }
            return sb.ToString ();
        }




        /// <summary>
        /// Регистрация игрока по его EMail'у
        /// </summary>
        /// <param name="email"></param>
        /// <param name="result"></param>
        public static void Register (string email, Action<bool, Response> result)
        {
            var request = Instance().CreateRequest("/player/registration/register");
            request.Form.AddField("email", email);
            StartAsyncRequest(
                request,
                res => result(true, res),
                res => result(false, res)
            );
        }

        /// <summary>
        /// Подключение профиля игрока по EMail'у и паролю
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <param name="result"></param>
        public static void Login (string email, string password, Action<bool, Response> result)
        {
            var request = Instance().CreateRequest("/player/login");
            request.Form.AddField("email", email);
            request.Form.AddField("password", password);
            StartAsyncRequest(
                request,
                res => result(true, res),
                res => result(false, res)
            );
        }


        /// <summary>
        /// Отметка о оставлении отзыва о игре
        /// </summary>
        /// <param name="successCallback"></param>
        /// <param name="failCallback"></param>
        public static void RateThisGame (Request.WWWResultCallback successCallback = null, Request.WWWResultCallback failCallback = null)
        {
            var request = Instance ().CreateRequest ("/player/rateGame");
            StartAsyncRequest (request, successCallback, failCallback);
        }

        public static void RateThisGameCancel (Request.WWWResultCallback successCallback = null, Request.WWWResultCallback failCallback = null)
        {
            var request = Instance ().CreateRequest ("/player/rateGameCancel");
            StartAsyncRequest (request, successCallback, failCallback);
        }

        public static void ChangeNickName (string newNickName, Request.WWWResultCallback successCallback = null, Request.WWWResultCallback failCallback = null)
        {
            var request = Instance ().CreateRequest ("/player/changeNickName");
            request.Form.AddField ("nick", newNickName);
            StartAsyncRequest (request, successCallback, failCallback);
        }

        public static void RejectNickName(Action<bool, Response> result)
        {
            var request = Instance().CreateRequest("/player/rejectNickName");
            StartAsyncRequest(
                request,
                res => result(true, res),
                res => result(false, res)
            );
        }

        /// <summary>
        /// Запрос начисления топлива в случае успешного приглашения друга в игру через соцсеть
        /// </summary>
        /// <param name="result">
        ///  bool - успешность операции (true - успех)
        /// </param>
        public static void FuelForInvite(Action<bool, Response> result)
        {
            var request = Instance().CreateRequest("/shop/fuelForInvite");
            StartAsyncRequest(
                request,
                res => result(true, res),
                res => result(false, res)
            );
        }

        /// <summary>
        /// Отправка на сервер лога о действии пользователя.
        /// </summary>
        /// <param name="location">
        /// Место действия (e.g. patternshop, decalshop, bank).
        /// </param>
        /// <param name="action">
        /// Действие (e.g. buyCamo, deliverModule).
        /// </param>
        /// <param name="query">
        /// Словарь данных для POST-запроса.
        /// </param>
        public static void ReportStats(string location, string action, Dictionary<string, string> query)
        {
            Request request = Instance().CreateRequest("/player/log");
            request.Form.AddField("location", location);
            request.Form.AddField("action", action);
            request.Form.AddField("timestamp", ((long)GameData.CurrentTimeStamp).ToString());

            foreach (var keyValuePair in query)
                request.Form.AddField(keyValuePair.Key, keyValuePair.Value);

            StartAsyncRequest(
                request
                //,
                //successCallback: result => Debug.Log("Stats successfully submitted to the server."),
                //failCallback: result => Debug.LogWarning("Stats' submission request failed with error: " + result.error)
                );
        }

        /// <summary>
        /// НЕ ИСПОЛЬЗОВАТЬ ПО ПУСТЯКАМ!
        /// Логирование важных для анализа сбоев исключений.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="e"></param>
        public static void ReportException (string action, System.Exception e)
        {
            ReportStats ("Exception", action, new Dictionary<string, string> {
                {"Message", e.Message},
                {"Source", e.Source},
                {"StackTrace", e.StackTrace}
            });
        }

        static public void dbg (string message)
        {
            if (Manager.s_useDebugOutput || ProfileInfo.isServerLogsEnabled) {
                foreach (string msg in splitMessage(message))
                    Debug.Log (msg);
            }
        }

        static public void dbgWarn (string message)
        {
            if (Manager.s_useDebugOutput || ProfileInfo.isServerLogsEnabled) {
                foreach (string msg in splitMessage(message))
                    Debug.LogWarning (msg);
            }
        }

        static private string[] splitMessage (string message)
        {
            int partsAmount = message.Length / DEBUG_MESSAGE_MAX_LENGTH;

            int remainder = message.Length % DEBUG_MESSAGE_MAX_LENGTH;

            string[] result = new string[remainder > 0 ? (partsAmount + 1) : partsAmount];

            for (int i = 0; i < partsAmount; i++)
                result[i] = message.Substring(DEBUG_MESSAGE_MAX_LENGTH * i, DEBUG_MESSAGE_MAX_LENGTH);

            if (remainder > 0)
                result[partsAmount] = message.Substring(DEBUG_MESSAGE_MAX_LENGTH * partsAmount, remainder);

            return result;
        }

        public static void OpenURL(string url)
        {
            var staticPart = Http.Manager.protocol
                + "://" + Http.Manager.Instance().server
                + "{0}"
                + "?token="
                + Http.Manager.Instance().sessionToken;

            string fullUrl = string.Format(staticPart, url);

            dbg("Opening URL: " + fullUrl);

#if UNITY_WEBPLAYER || UNITY_WEBGL
            WebTools.OpenURL(fullUrl);
#else
            Application.OpenURL(fullUrl);
#endif
        }

        public static string LocalizeServerErrCode(Error err)
        {
            string key = "serverErrCode_" + (int)err;
            return key;
        }
    }
}
