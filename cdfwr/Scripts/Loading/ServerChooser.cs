using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace XDevs.Loading {
    public class ServerChooser : MonoBehaviour {

        public const string PREFS_KEY = "CurrentServer";

        public static void ClearChoose () {
            PlayerPrefs.DeleteKey (PREFS_KEY);
            PlayerPrefs.Save ();
        }

        public static Http.Server CurrentServer {
            get {

                return currentServer;
            }
        }

        private static Http.Server currentServer = null;

        [SerializeField]
        private tk2dUILayoutContainerSizer buttons;
        [SerializeField]
        private GameObject buttonPrefab;
        [SerializeField]
        private GameObject wrapper;

        private List<ProfileServer> servers = new List<ProfileServer> ();

        void Awake () {
            if (GameData.ServerDataReceived) {
                return;
            }
            Dispatcher.Subscribe (EventId.AfterWebPlatformDefined, StartInit, 1);
        }

        void OnDestroy () {
            Dispatcher.Unsubscribe (EventId.AfterWebPlatformDefined, StartInit);
        }

        void StartInit (EventId id, EventInfo info) {
            List<Http.Server> servers = Http.Manager.Instance().GetAvailableServers();
            if (servers.Count <= 0) {
                Debug.LogError ("No any server available for connecting!");
                return;
            }

            // Миграция старых клиентов на новое место сохранения GUID'а профиля
            if (PlayerPrefs.HasKey ("GUID")) {
                PlayerPrefs.SetString ("GUID_" + WorldRegion.Europe, PlayerPrefs.GetString("GUID")); // Пересохрание GUID'а в новое место
                PlayerPrefs.DeleteKey ("GUID");
                PlayerPrefs.SetInt (PREFS_KEY, (int)WorldRegion.Europe); // Выставление евро сервера как выбранного
                PlayerPrefs.Save ();
            }

            if (servers.Count == 1) {
                ServerChoosed (servers[0]);
                return;
            }

            if (PlayerPrefs.HasKey (PREFS_KEY)){
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

            if (this.servers.Count > 0) {
                return;
            }

            foreach (Http.Server s in servers) {
                var p = ProfileServer.Create (buttonPrefab, buttons, s);
                p.OnClicked += ServerChoosed;
                this.servers.Add (p);
            }

#if !UNITY_WEBGL
            StartCoroutine (ReceiveRegions ());
#endif
        }


        void ServerChoosed (Http.Server server) {
            Debug.LogFormat ("Server choosed: {0} - {1}", server.region, server.Url);
            PlayerPrefs.SetInt (PREFS_KEY, (int)server.region);
            currentServer = server;
            XdevsSplashScreen.SetActive (true);
            XdevsSplashScreen.SetActiveWaitingIndicator (true);
            wrapper.SetActive (false);
            Http.Manager.Instance ().Region = server.region;
            Dispatcher.Send (EventId.ProfileServerChoosed, new EventInfo_SimpleEvent ());
        }


        IEnumerator ReceiveRegions () {
            PhotonNetwork.ConnectToBestCloudServer ("-1");
            while (PhotonNetwork.networkingPeer.AvailableRegions == null) {
                yield return null;
            }
            PhotonNetwork.Disconnect ();
        }
    }
}