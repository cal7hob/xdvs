using UnityEngine;
using System.Collections.Generic;
using System;

public class RadarController : MonoBehaviour
{
    public tk2dSprite sprRadarScreen;
    public RadarPoint radarPointPrefab;
    public float maxVisibleDistance;
    public float minVisibleDistance;
    public float minHeightDelta;
    public float maxHeightDelta;

    private static RadarController instance;

    private readonly Dictionary<int, RadarPoint> radarPoints = new Dictionary<int, RadarPoint>();
    private int chatMessagePhotonPlayerId = 0;
    private int chatMessageId = 0;

    public static float DisplayRatio
    {
        get; private set;
    }

    public static float MinHeightDelta
    {
        get {return instance.minHeightDelta;}
    }

    public static float MaxHeightDelta
    {
        get { return instance.maxHeightDelta; }
    }

    public static float MaxVisibleDistance
    {
        get { return instance.maxVisibleDistance; }
    }

    public static float MaxPointDistance
    {
        get; private set;
    }

    public static float MinVisibleDistance
    {
        get { return instance.minVisibleDistance; }
    }

    void Awake()
    {
        if (instance)
        {
            Debug.LogError("There is more than one RadarController on the scene. Disabling.", gameObject);
            gameObject.SetActive(false);
            return;
        }

        instance = this;

        MaxPointDistance = instance.sprRadarScreen.GetBounds().extents.x;

        DisplayRatio = MaxPointDistance / maxVisibleDistance;

        Dispatcher.Subscribe(EventId.TankJoinedBattle, OnTankConnected);
        Dispatcher.Subscribe(EventId.TankLeftTheGame, OnTankLeftTheGame);
        Dispatcher.Subscribe(EventId.TankAvailabilityChanged, OnTankAvailabilityChanged);
        Dispatcher.Subscribe(EventId.SecondaryWeaponUsed, OnSecondaryWeaponUsed);
        if (radarPointPrefab.chatMessageWrapper)
            Dispatcher.Subscribe(EventId.BattleChatCommand, OnBattleChatCommand);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TankJoinedBattle, OnTankConnected);
        Dispatcher.Unsubscribe(EventId.TankLeftTheGame, OnTankLeftTheGame);
        Dispatcher.Unsubscribe(EventId.TankAvailabilityChanged, OnTankAvailabilityChanged);
        Dispatcher.Unsubscribe(EventId.SecondaryWeaponUsed, OnSecondaryWeaponUsed);
        Dispatcher.Unsubscribe(EventId.BattleChatCommand, OnBattleChatCommand);
    }
    
    private void OnTankConnected(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;

        if (BattleController.allVehicles[info.int1].IsMain)
            return;

        RadarPoint point = Instantiate(radarPointPrefab);

        point.transform.parent = sprRadarScreen.transform;
        point.transform.localPosition = Vector3.zero;
        point.Target = BattleController.allVehicles[info.int1];

        radarPoints.Add(info.int1, point);
    }

    private void OnTankLeftTheGame(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;

        RadarPoint point;

        if (!radarPoints.TryGetValue(info.int1, out point))
            return;

        radarPoints.Remove(info.int1);

        Destroy(point.gameObject);
    }
    
    private void OnTankAvailabilityChanged(EventId id, EventInfo ei)
    {
        EventInfo_II info = (EventInfo_II)ei;

        RadarPoint point;

        if (!radarPoints.TryGetValue(info.int1, out point))
            return;

        point.gameObject.SetActive(info.int2 == 1);
    }

    private void OnSecondaryWeaponUsed(EventId eid, EventInfo ei)
    {
        EventInfo_IIIV info = (EventInfo_IIIV)ei;

        if (info.int1 != BattleController.MyPlayerId && info.int3 != BattleController.MyPlayerId)
            return;

        RadarPoint point;

        if (!radarPoints.TryGetValue(info.int1, out point) && !radarPoints.TryGetValue(info.int3, out point))
            return;

        foreach (var radarPoint in radarPoints)
            radarPoint.Value.IsMain = false;

        point.IsMain = true;
    }

    private RadarPoint GetItem(int photonPlayerId)
    {
        if (radarPoints == null || !radarPoints.ContainsKey(photonPlayerId))
            return null;
        return radarPoints[photonPlayerId];
    }

    private void OnBattleChatCommand(EventId id, EventInfo info)
    {
        EventInfo_U eventData = (EventInfo_U)info;
        chatMessagePhotonPlayerId = Convert.ToInt32(eventData[0]);
        chatMessageId = Convert.ToInt32(eventData[1]);

        RadarPoint rp = GetItem(chatMessagePhotonPlayerId);
        if (!rp || !rp.gameObject.activeInHierarchy)
            return;

        rp.SetupChatMessage(new BattleChatPanelItemData(chatMessagePhotonPlayerId, (BattleChatCommands.Id)chatMessageId, Time.realtimeSinceStartup));
    }
}