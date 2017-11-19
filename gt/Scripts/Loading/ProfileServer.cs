using UnityEngine;
using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;

namespace XDevs.Loading {

    public class ProfileServer : MonoBehaviour {

        public event Action<Http.Server> OnClicked = delegate(Http.Server s) { };


        [SerializeField]
        private tk2dTextMesh serverName;
        [SerializeField]
        private ActivatedUpDownButton button;
        [SerializeField]
        private PingIndicator pingIndicator;


        private Http.Server server;

        public static ProfileServer Create (GameObject prefab, tk2dUILayoutContainerSizer parent, Http.Server server) {
            var button = Instantiate (prefab, parent.transform) as GameObject;
            button.name = prefab.name + "_" + server.region;
            parent.AddLayout (button.GetComponent<tk2dUILayout> (), new tk2dUILayoutItem ());

            var p = button.GetComponent<ProfileServer> ();
            p.Init (server);


            return p;
        }

        public void RegionsReceived (List<Region> regions) {

        }


        void Init (Http.Server server) {
            serverName.gameObject.name = serverName.gameObject.name + "_" + server.region.ToString ();
            serverName.text = server.region.ToString ();
            serverName.gameObject.AddComponent<LabelLocalizationAgent> ();

            this.server = server;

            GetComponent<tk2dUIItem> ().OnClick += ButtonClicked;
            button.Activated = false;

#if UNITY_WEBGL
            pingIndicator.gameObject.SetActive (false);
#endif
            StartCoroutine (ServerStatusRequest ());
        }

        void ButtonClicked () {
            OnClicked (server);
        }

        IEnumerator ServerStatusRequest () {
            WWW www = new WWW (server.Url + "/options/photon");
            float connectionTime = 0;
            while (!www.isDone) {
                connectionTime += Time.deltaTime;
                if (connectionTime > 10.0f) {
                    connectionTime = 0;
                    break;
                }
                yield return null;
            }
            if (!string.IsNullOrEmpty (www.error)) {
                Debug.LogWarningFormat ("Server {0} with Url {1} is unavalable! Error: {2}", server.region, server.Url, www.error);
                yield break;
            }

            button.Activated = true;

#if !UNITY_WEBGL
            JsonPrefs prefs = new JsonPrefs (www.text);
            CloudRegionCode regionCode = Region.Parse (prefs.ValueString("region", "none"));
            if (regionCode == CloudRegionCode.none) {
                Debug.LogErrorFormat ("Server {0} with Url {1}: Wrong photon region! JSON: ", server.region, server.Url, www.text);
                yield break;
            }


            while (PhotonNetwork.networkingPeer.AvailableRegions == null) {
                yield return null;
            }

            Region region = PhotonNetwork.networkingPeer.AvailableRegions.Find (r => r.Code == regionCode);
            if (region == null) {
                Debug.LogErrorFormat ("Server {0} with Url {1}: Photon region disabled in settings!", server.region, server.Url, regionCode);
                yield break;
            }

            PhotonPingManager manager = new PhotonPingManager ();
            yield return manager.PingSocket (region);

            pingIndicator.SetPing (region.Ping);
#endif
        }

    }
}