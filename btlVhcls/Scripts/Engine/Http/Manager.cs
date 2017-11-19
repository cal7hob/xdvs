using System.Collections.Generic;
using System.Text;
using System;
using UnityEngine;
using System.Linq;

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
        InternalDeploying = 1503,

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

    public class Server {
        public WorldRegion region;
        public string protocol;
        public string domain;
        public string photonPostfix;
        public string Url { get { return protocol + "://" + domain; } }

        public Server (WorldRegion region, string protocol, string domain, string photonPostfix) {
            this.region = region;
            this.protocol = protocol;
            this.domain = domain;
            this.photonPostfix = photonPostfix;
#if UNITY_WEBGL && !UNITY_EDITOR
            this.protocol = "https";
#endif
        }
    }

    public class Manager : MonoBehaviour
    {
        public static string CurrentServer { get { return Instance ().protocol + "://" + Instance ().server; } }
        public static BattleServer BattleServer { get { return Instance ().battleServer; } }

        public const string ROUTE_PROFILE = "/public/profile/";//открытие личного кабинета
        public const string ROUTE_RESETPASSWORD = "/public/resetPassword/";
        public const string ROUTE_SUPPORT = "/public/profile/feedback"; // Техподдержка, кнопка btnSupport

#region Servers list
        private static Dictionary<Interface, List<Server>> servers = new Dictionary<Interface, List<Server>>
        {
            { Interface.IronTanks, new List<Server> {
                    new Server (WorldRegion.Debug,  "http",  "irontanks.scifi-tanks.com", "d"),
                    new Server (WorldRegion.Europe, "https", "irontanks.extreme-developers.com", "e")
                }
            },
            { Interface.FutureTanks, new List<Server> {
                    new Server (WorldRegion.Debug,  "http",  "futuretanks.scifi-tanks.com", "d"),
                    new Server (WorldRegion.Europe, "https", "futuretanks.extreme-developers.com", "e")
                }
            },
            { Interface.ToonWars, new List<Server> {
                    new Server (WorldRegion.Debug,  "http",  "toonwars.scifi-tanks.com", "d"),
                    new Server (WorldRegion.Europe, "https", "toonwars.extreme-developers.com", "e")
                }
            },
            { Interface.BlowOut, new List<Server> {
                    new Server (WorldRegion.Debug,  "http",  "blowout.scifi-tanks.com", "d"),
                    new Server (WorldRegion.Europe, "https", "blowout.extreme-developers.com", "e")
                }
            },
            { Interface.SpaceJet, new List<Server> {
                    new Server (WorldRegion.Debug,  "http",  "spacejet.scifi-tanks.com", "d"),
                    new Server (WorldRegion.Europe, "https", "spacejet3d.extreme-developers.com", "e")
                }
            },
            { Interface.BattleOfWarplanes, new List<Server> {
                    new Server (WorldRegion.Debug,  "http",  "bow.scifi-tanks.com",                         "d"),
                    new Server (WorldRegion.Europe, "https", "battleofwarplanes.extreme-developers.com",    "e"),
                    new Server (WorldRegion.Asia,   "https", "battleofwarplanes-sg.extreme-developers.com", "a")
                }
            },
            { Interface.BattleOfHelicopters, new List<Server> {
                    new Server (WorldRegion.Debug,  "http",  "boh.scifi-tanks.com", "d"),
                    new Server (WorldRegion.Europe, "https", "battleofhelicopters.extreme-developers.com", "e")
                }
            },
            { Interface.Armada, new List<Server> {
                    new Server (WorldRegion.Debug,  "http",  "armada.scifi-tanks.com", "d"),
                    new Server (WorldRegion.Europe, "https", "armada.extreme-developers.com", "e"),
                    new Server (WorldRegion.Asia,   "https", "armada-sg.extreme-developers.com", "a"),
                }
            },
            { Interface.MetalForce, new List<Server> {
                    new Server (WorldRegion.Debug,  "http",  "metalforce.scifi-tanks.com",              "d"),
                    new Server (WorldRegion.Europe, "https", "metalforce-eu.extreme-developers.com",    "e"),
                    new Server (WorldRegion.Asia,   "https", "metalforce-sg.extreme-developers.com",    "a")
                }
            },
            { Interface.WingsOfWar, new List<Server> {
                    new Server (WorldRegion.Debug,  "http",  "wow.scifi-tanks.com", "d"),
                    new Server (WorldRegion.Europe, "https", "wingsofwar.extreme-developers.com", "e")
                }
            },
        };
#endregion

        public static string signatureHeader = "X-SIGNATURE";

        public static string Protocol { get { return Instance ().protocol; } }
        public static string Server { get { return Instance ().server; } }
        public static string PhotonPostfix { get { return Instance ().photonPostfix; } }
        public static string SessionToken {
            get { return Instance ().sessionToken; }
            set { Instance ().sessionToken = value; }
        }
        public static int RequestNum {
            get { return Instance ().requestNum; }
            set { Instance ().requestNum = value; }
        }
        // Current server
        private string protocol;
        private string server;
        private string photonPostfix;
        public WorldRegion Region {
            get { return region; }
            set {
                region = value;
                protocol = GetProtocol ();
                server = GetServer ();
                photonPostfix = GetPhotonPostfix ();
            }
        }
        public bool useDebugOutput = false;

        private const int DEBUG_MESSAGE_MAX_LENGTH = 16300;

        // Debug output on true
        public static bool s_useDebugOutput = false;

        private WorldRegion region = WorldRegion.Europe;
        private string sessionToken = "";
        private int requestNum = 0;

        private BattleServer battleServer;

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

        public List<Server> GetAvailableServers () {
            if (Debug.isDebugBuild) { 
                return servers[GameData.CurInterface];
            }
            else {
                return servers[GameData.CurInterface].FindAll (s => s.region != WorldRegion.Debug);
            }
        }

        private string GetProtocol () {
            return servers[GameData.CurInterface].Find (s => s.region == region).protocol;
        }

        private string GetServer()
        {
            return servers[GameData.CurInterface].Find (s => s.region == region).domain;
        }

        public static Server GetServer(Interface iface, WorldRegion region)
        {
            return servers[iface].Find(s => s.region == region);
        }

        private string GetPhotonPostfix () {
            return servers[GameData.CurInterface].Find (s => s.region == region).photonPostfix;
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

            if (string.IsNullOrEmpty (server)) {
                server = GetServer ();
                photonPostfix = GetPhotonPostfix ();
            }
#if UNITY_WEBGL && !UNITY_EDITOR
            foreach (var s in servers[GameData.CurInterface]) {
                s.protocol = "https";
            }
#endif

#if UNITY_EDITOR
            s_useDebugOutput = useDebugOutput;
#endif
        }

#if UNITY_EDITOR
        public void OpenPlayerProfile ()
        {
            var url = CurrentServer + "/admin/player/" + ProfileInfo.profileId;
            Debug.Log ("Open URL: " + url);
            Application.OpenURL (url);
        }
#endif

        public Request CreateRequest (string path) {
            return new Request (CurrentServer + path);
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
            if (Instance().useDebugOutput || ProfileInfo.isServerLogsEnabled) {
                foreach (string msg in splitMessage(message))
                    Debug.Log (msg);
            }
        }

        static public void dbgWarn (string message)
        {
            if (Instance().useDebugOutput || ProfileInfo.isServerLogsEnabled) {
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
            var staticPart = Http.Manager.Protocol
                + "://" + Http.Manager.Instance().server
                + "{0}"
                + "?token="
                + Http.Manager.SessionToken;

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
            return Localizer.Loaded ?
                (Localizer.ContainsKey(key) ? Localizer.GetText(key) : Localizer.GetText("ApplicationError", ((int)err).ToString())) :
                ("Error " + ((int)err).ToString());
        }
    }
}
