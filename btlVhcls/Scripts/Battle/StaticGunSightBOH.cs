using UnityEngine;

public class StaticGunSightBOH : MonoBehaviour
{
    public tk2dSprite sprStaticGunSight;
    public Color activeColor;
    public Color inactiveColor;

    void Awake()
    {
        Dispatcher.Subscribe(EventId.TargetAimed, OnTargetAimed);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TargetAimed, OnTargetAimed);
    }

    private void OnTargetAimed(EventId id, EventInfo ei)
    {
        EventInfo_IIB info = (EventInfo_IIB)ei;

        if (((EventInfo_IIB)ei).int1 != BattleController.MyPlayerId)
            return;

        bool aimed = info.bool1;

        sprStaticGunSight.color = aimed ? inactiveColor : activeColor;

        sprStaticGunSight.gameObject.SetActive(!aimed);
    }
}
