using UnityEngine;
using System.Collections;
using Disconnect;

public class HighPingDetector : MonoBehaviour
{

    public static bool isEnabled = true;

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
        int detectionCount = Mathf.RoundToInt(GameData.pingDetectionPeriod / GameData.pingDetectionInterval);
        int maxFailureCount = Mathf.Clamp(detectionCount / 2 - 1, 1, detectionCount);
        YieldInstruction wait = new WaitForSeconds(GameData.pingDetectionInterval);

        bool pingAlertEnabled = false;
        while (PhotonNetwork.connected)
        {
            if (!isEnabled) continue;
            int failureCount = 0;
            for (int i = 1; i <= detectionCount; i++)
            {
                if (!isEnabled) break;
                if (PhotonNetwork.GetPing() > GameData.MaxPing && ++failureCount > maxFailureCount)
                {
                    if (BattleConnectManager.IsMasterClient)
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

    private void OnApplicationPause (bool pause) {
        if ( (PhotonNetwork.BackgroundTimeout <= 0.1f) || !GameData.AllowBackgroundConnection) return;
        isEnabled = !pause;
    }
}
