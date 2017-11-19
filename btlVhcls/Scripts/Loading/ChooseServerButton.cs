using UnityEngine;
using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;

namespace XDevs.Loading
{
    public class ChooseServerButton : MonoBehaviour, IItem
    {
        [SerializeField] private tk2dSlicedSprite sizeBg;
        [SerializeField] private tk2dTextMesh lblServerName;
        [SerializeField] private tk2dTextMesh lblPing;
        [SerializeField] private tk2dTextMesh lblPingVal;
        [SerializeField] private ActivatedUpDownButton recommendedDisabler;
        [SerializeField] private ActivatedUpDownButton infoReceivedActivator;//Блокируем нажатия, показываем WaitingIndicator, скрываем элементы
        [SerializeField] private PingIndicator pingIndicator;

        private Http.Server server;
        private ServerChooser serverChooser;

        public int Ping { get; private set; }
        public bool IsRecommended { get { return recommendedDisabler.Activated; } set { recommendedDisabler.Activated = value; } }

        public void Initialize(object[] parameters)
        {
            server = (Http.Server)parameters[0];
            ParamDict additionalParams = (ParamDict)parameters[1];
            serverChooser = (ServerChooser)additionalParams["ServerChooser"];

            transform.name = server.region.ToString();
            infoReceivedActivator.Activated = false;
            IsRecommended = false;
            Ping = 999999;//Чтоб если не получил ответ - был в конце списка
            lblServerName.gameObject.name = lblServerName.gameObject.name + "_" + server.region.ToString();
            lblServerName.text = server.region.ToString();
            lblPing.text = string.Format("{0}:", Localizer.GetText("lblPing"));

            UpdateElements();

            StartCoroutine(ServerStatusRequest());
        }

        private void OnClick(tk2dUIItem btn)
        {
            serverChooser.ServerChoosed(server);
        }

        IEnumerator ServerStatusRequest()
        {
            WWW www = new WWW(server.Url + "/options/photon");
            float connectionTime = 0;
            while (!www.isDone)
            {
                connectionTime += Time.deltaTime;
                if (connectionTime > 10.0f)
                {
                    connectionTime = 0;
                    break;
                }
                yield return null;
            }
            if (!string.IsNullOrEmpty(www.error))
            {
                Debug.LogWarningFormat("Server {0} with Url {1} is unavalable! Error: {2}", server.region, server.Url, www.error);
                yield break;
            }



#if !UNITY_WEBGL
            JsonPrefs prefs = new JsonPrefs(www.text);
            CloudRegionCode regionCode = Region.Parse(prefs.ValueString("region", "none"));
            if (regionCode == CloudRegionCode.none)
            {
                Debug.LogErrorFormat("Server {0} with Url {1}: Wrong photon region! JSON: {2}", server.region, server.Url, www.text);
                yield break;
            }

            while (PhotonNetwork.networkingPeer.AvailableRegions == null)
            {
                yield return null;
            }

            Region region = PhotonNetwork.networkingPeer.AvailableRegions.Find(r => r.Code == regionCode);
            if (region == null)
            {
                Debug.LogErrorFormat("Server {0} with Url {1}: Photon region disabled in settings!", server.region, server.Url, regionCode);
                yield break;
            }

            PhotonPingManager manager = new PhotonPingManager();
            yield return manager.PingSocket(region);

            Ping = region.Ping;
            pingIndicator.SetPing(Ping);
            lblPingVal.text = MiscTools.GetCultureSpecificFormatOfNumber(Ping);
            serverChooser.OnServerPingReceived();

            infoReceivedActivator.Activated = true;
#else
            //На webgl не работает функция определения пинга в фотоне
            infoReceivedActivator.Activated = true;
            pingIndicator.gameObject.SetActive(false);
#endif
        }

        public void UpdateElements()
        {

        }

        public void DesrtoySelf()
        {
        }

        public Vector2 GetSize()
        {
            return new Vector2(sizeBg.dimensions.x * sizeBg.scale.x, sizeBg.dimensions.y * sizeBg.scale.y);
        }

        public string GetUniqId { get { return server.region.ToString(); } }

        public tk2dUIItem MainUIItem { get { return null; } }

        public Transform MainTransform { get { return transform; } }

    }
}