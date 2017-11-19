using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace XDevs.Loading
{
    public class ServerChooser : MonoBehaviour
    {
        [SerializeField] private GameObject wrapper;
        [SerializeField] private tk2dSlicedSprite bg;
        [SerializeField] private tk2dSlicedSprite headerBg;
        [SerializeField] private UniAligner aligner;
        [SerializeField] private Factory factory;

        public const string PREFS_KEY = "CurrentServer";

        private static Http.Server currentServer = null;

        public static Http.Server CurrentServer { get { return currentServer; } }

        private bool isWebPlatformDefined = false;
        private bool isGameDataInitialized = false;
        private bool isStartInitEcecuted = false;//Страховка от повторного вызова

        void Awake ()
        {
            if (GameData.ServerDataReceived) 
                return;
            
            Dispatcher.Subscribe (EventId.AfterWebPlatformDefined, AfterWebPlatformDefined, 1);
            Dispatcher.Subscribe (EventId.ResolutionChanged, OnResolutionChanged);
        }

        void OnDestroy ()
        {
            Dispatcher.Unsubscribe (EventId.AfterWebPlatformDefined, AfterWebPlatformDefined);
            Dispatcher.Unsubscribe (EventId.ResolutionChanged, OnResolutionChanged);
        }

        private IEnumerator Start ()
        {
            while (!Localizer.Loaded)
                yield return null;

            while (XdevsSplashScreen.Instance == null)
                yield return null;

            isGameDataInitialized = true;
            ExecuteStartInitIfCan();
        }

        void AfterWebPlatformDefined(EventId id, EventInfo info)
        {
            isWebPlatformDefined = true;
            ExecuteStartInitIfCan();
        }

        void ExecuteStartInitIfCan()
        {
            if (isWebPlatformDefined && isGameDataInitialized && !isStartInitEcecuted) {
                StartInit();
            }
        }

        void StartInit ()
        {
            isStartInitEcecuted = true;
            List<Http.Server> servers = Http.Manager.Instance().GetAvailableServers();
            if (servers.Count <= 0)
            {
                Debug.LogError ("No any server available for connecting!");
                return;
            }

            // Миграция старых клиентов на новое место сохранения GUID'а профиля
            if (PlayerPrefs.HasKey ("GUID"))
            {
                PlayerPrefs.SetString ("GUID_" + WorldRegion.Europe, PlayerPrefs.GetString("GUID")); // Пересохрание GUID'а в новое место
                PlayerPrefs.DeleteKey ("GUID");
                PlayerPrefs.SetInt (PREFS_KEY, (int)WorldRegion.Europe); // Выставление евро сервера как выбранного
                PlayerPrefs.Save ();
            }

            if (servers.Count == 1)
            {
                ServerChoosed (servers[0]);
                return;
            }

            if (PlayerPrefs.HasKey (PREFS_KEY))
            {
                WorldRegion selectedRegion = (WorldRegion)PlayerPrefs.GetInt (PREFS_KEY);
                var found = servers.Find (s => s.region == selectedRegion);
                if (found != null) {
                    ServerChoosed (found);
                    return;
                }
            }

            wrapper.SetActive (true);
            XdevsSplashScreen.SetActive (false);
            XdevsSplashScreen.SetActiveWaitingIndicator (false);

            if (factory.Items.Count > 0)
                return;

            factory.CreateAll(servers, true, new ParamDict().Add("ServerChooser", this));
            UpdateWindow();

            #if !UNITY_WEBGL
            StartCoroutine (ReceiveRegions ());
            #endif
        }

        public static void ClearChoose()
        {
            PlayerPrefs.DeleteKey(PREFS_KEY);
            PlayerPrefs.Save();
        }

        public void ServerChoosed (Http.Server server)
        {
            Debug.LogFormat ("Server choosed: {0} - {1}", server.region, server.Url);
            PlayerPrefs.SetInt (PREFS_KEY, (int)server.region);
            currentServer = server;
            XdevsSplashScreen.SetActive (true);
            XdevsSplashScreen.SetActiveWaitingIndicator (true);
            wrapper.SetActive (false);
            Http.Manager.Instance ().Region = server.region;
            Dispatcher.Send (EventId.ProfileServerChoosed, new EventInfo_SimpleEvent ());
        }

        IEnumerator ReceiveRegions ()
        {
            PhotonNetwork.ConnectToBestCloudServer ("-1");
            while (PhotonNetwork.networkingPeer.AvailableRegions == null) {
                yield return null;
            }
            PhotonNetwork.Disconnect ();
        }

        /// <summary>
        /// Enable "Recommended" text for a fastest server only
        /// </summary>
        public void OnServerPingReceived()
        {
            for (int i = 0; i < factory.Items.Count; i++)
                ((ChooseServerButton)factory.Items[i]).IsRecommended = false;

            List<IItem> sorted = factory.Items.OrderBy(item => ((ChooseServerButton)item).Ping).ToList();
            if (sorted.Count > 0)
                ((ChooseServerButton)sorted[0]).IsRecommended = true;
        }

        private void UpdateWindow()
        {
            bg.dimensions = new Vector2(bg.dimensions.x, (factory.ScrollableArea.ContentLength + headerBg.dimensions.y * headerBg.scale.y) / bg.scale.y);
            aligner.Align();
        }

        void OnResolutionChanged(EventId id, EventInfo info)
        {
            UpdateWindow();
        }
    }
}