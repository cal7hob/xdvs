using UnityEngine;
using System.Collections;
using Disconnect;

public class HighPingDetector : MonoBehaviour
{
    void Awake()
    {
        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Subscribe(EventId.BeforeReconnecting, BeforeReconnect);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Unsubscribe(EventId.BeforeReconnecting, BeforeReconnect);
    }

    private IEnumerator Detecting()
    {
        //if (Debug.isDebugBuild)
        //    yield break;

        int detectionCount = Mathf.RoundToInt(GameData.pingDetectionPeriod / GameData.pingDetectionInterval);
        int maxFailureCount = detectionCount / 2 + 1;
        YieldInstruction wait = new WaitForSeconds(GameData.pingDetectionInterval);

        bool pingAlertEnabled = false;
        while (PhotonNetwork.connected)
        {
            int failureCount = 0;
            for (int i = 1; i <= detectionCount; i++)
            {
                if (PhotonNetwork.GetPing() > GameData.MaxPing && ++failureCount > maxFailureCount)
                {
                    if (PhotonNetwork.isMasterClient)
                        PhotonNetwork.Disconnect();
                    else
                        BattleConnectManager.Instance.ForcedDisconnect();
                    yield break;
                }

                bool highPingByMedian = failureCount > i / 2;
                if (pingAlertEnabled != highPingByMedian && i >= detectionCount / 2)
                {
                    pingAlertEnabled = highPingByMedian;
                    Dispatcher.Send(EventId.HighPingAlarm, new EventInfo_B(pingAlertEnabled));
                }

                yield return wait;
            }
        }
    }

  

    private void OnMainTankAppeared(EventId id, EventInfo ei)
    {
        if (!PhotonNetwork.offlineMode)
            StartCoroutine(Detecting());
    }

    private void BeforeReconnect(EventId id, EventInfo ei)
    {
        StopAllCoroutines();
    }
}
