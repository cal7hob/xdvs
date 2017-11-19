using DG.Tweening;
using UnityEngine;

public class StaticGunSightBOH : MonoBehaviour
{
    public tk2dSlicedSprite sprStaticGunSight;
    public Color activeColor;
    public Color inactiveColor;
    public Color shotColor;
    public float timeOfShotColor = 0.2f;
    public float scaleBig = 2.5f;
    public float scaleNormal = 1f;
    private float currentTimeToShotColor = 0;
    private bool aimed;
    private bool iDamageTarget;
    private int target;
    private const float gunsighNormalizeSpeed = 5f;

    void Awake()
    {
        Dispatcher.Subscribe(EventId.TargetAimed, OnTargetAimed);
        Dispatcher.Subscribe(EventId.MyTankShots, MyTankShots);
        Dispatcher.Subscribe(EventId.TankTakesDamage, TargetTakesDamage);

    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TargetAimed, OnTargetAimed);
        Dispatcher.Unsubscribe(EventId.MyTankShots, MyTankShots);
        Dispatcher.Unsubscribe(EventId.TankTakesDamage, TargetTakesDamage);
    }

    private void OnTargetAimed(EventId id, EventInfo ei)
    {
        EventInfo_IIB info = (EventInfo_IIB)ei;

        if (((EventInfo_IIB)ei).int1 != BattleController.MyPlayerId)
            return;
        aimed = info.bool1;
        target = ((EventInfo_IIB)ei).int2;
        ChangeColor();

    }

    private void ChangeColor()
    {
        if (aimed && iDamageTarget)
        {
            sprStaticGunSight.color = shotColor;
        }
        else if (aimed && !iDamageTarget)
        {
            sprStaticGunSight.color = inactiveColor;
        }
        else
        {
            sprStaticGunSight.color = activeColor;
        }
    }

    private void MyTankShots(EventId _id, EventInfo _info)
    {
        ChangeColor();
        DOTween.CompleteAll();
        sprStaticGunSight.scale = new Vector3(scaleBig, scaleBig, sprStaticGunSight.scale.z);
    }

    private void TargetTakesDamage(EventId _id, EventInfo _info)
    {
        //TankTakesDamage = 1501, // victim, damage, attacker, shellType, position (Vector3)
        EventInfo_U info = (EventInfo_U)_info;
        if ((int)info[0] != target || (int)info[2] != BattleController.MyPlayerId)
        {
            return;
        }
        if (BattleController.allVehicles[target].data.armor <= 0) // Не красим прицел на трупах.
        {
            return;
        }
        if (aimed)
        {
            iDamageTarget = true;
            currentTimeToShotColor = timeOfShotColor;
        }
    }

    void Update()
    {
        if (currentTimeToShotColor > 0)
        {
            currentTimeToShotColor -= Time.deltaTime;
            if (currentTimeToShotColor <= 0)
            {
                iDamageTarget = false;
                ChangeColor();
            }
        }
        if (sprStaticGunSight.scale.x > scaleNormal)
        {
            //DOTween.To(() => sprStaticGunSight.scale, x => sprStaticGunSight.scale = x, Vector3.one, 1); Жретъ
            var deltaTime = Time.deltaTime;
            sprStaticGunSight.scale = new Vector3(sprStaticGunSight.scale.x - deltaTime * gunsighNormalizeSpeed,
                sprStaticGunSight.scale.y - deltaTime * gunsighNormalizeSpeed, sprStaticGunSight.scale.z);
        }

    }
}
