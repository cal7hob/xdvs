using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;

using XDevs.LiteralKeys;

public class SACLOSMissile : Shell
{
    public float accelerationRatio = 0.25f;

    private const float MIN_ROTATION_SPEED = 15.0f;
    private const float MAX_ROTATION_SPEED = 1000.0f;

    private static readonly ObscuredFloat MIN_DISTANCE = 5.0f;
    private static readonly ObscuredFloat RADIUS_RATIO = 1.15f;

    private bool isMisguided;
    private float currentSpeed;
    private float currentRotationSpeed;
    private float currentDistanceToTarget;
    private float previousDistanceToTarget;
    private Vector3 targetPosition;
    private IRCM victimIRCM;

    void Awake()
    {
        Dispatcher.Subscribe(EventId.ShellStateChanged, OnShellStateChanged);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Dispatcher.Unsubscribe(EventId.ShellStateChanged, OnShellStateChanged);
    }

    protected override void Update()
    {
        VehicleController victim = BattleController.allVehicles.GetValueOrDefault(victimId);

        if (flightDistance > shellItem.maxDistance || victim == null || victim.Armor <= 0)
        {
            ReturnObject();
            return;
        }

        currentSpeed
            = Mathf.Lerp(
                a:  currentSpeed,
                b:  OwnerSpeed + shellItem.speed,
                t:  accelerationRatio);

        deltaPos = currentSpeed * Time.deltaTime;

        if (isMisguided)
        {
            if (victimIRCM != null)
            {
                targetPosition = victimIRCM.transform.position;
                currentRotationSpeed = MIN_ROTATION_SPEED;
            }
        }
        else
        {
            targetPosition = victim.TargetPoint;
            currentRotationSpeed = MAX_ROTATION_SPEED;
        }

        currentDistanceToTarget = Vector3.Distance(transform.position, targetPosition);

        bool isCloseToTarget = currentDistanceToTarget <= MIN_DISTANCE;
        bool isFlyingAround = (previousDistanceToTarget - currentDistanceToTarget) < deltaPos;

        RaycastHit hit;

        if (IsLocal)
        {
            if (isMisguided && (victimIRCM == null || isCloseToTarget || isFlyingAround))
            {
                Explosion(targetPosition);
                return;
            }

            if (Physics.SphereCast(
                /* origin:      */  transform.position,
                /* radius:      */  deltaPos * RADIUS_RATIO,
                /* direction:   */  transform.forward,
                /* hitInfo:     */  out hit,
                /* maxDistance: */  deltaPos,
                /* layerMask:   */  hitMask))
            {
                VehicleController hitVictim = hit.collider.GetComponentInParent<VehicleController>();

                if (hitVictim == null)
                {
                    Explosion(hit.point);
                    return;
                }

                // Не даём принадлежащему мне (т.е. мастеру) боту хитануть себя (у него слой Enemy):
                bool victimIsMe = hitVictim.data.playerId == ownerId;

                if (!hitVictim.IsMainsFriend && !victimIsMe)
                {
                    ShellHitDispatcher.AccumulateDamage(
                        victimId:   hitVictim.data.playerId,
                        attackerId: ownerId,
                        damage:     Damage,
                        position:   hitVictim.transform.InverseTransformPoint(hit.point),
                        shellType:  shellType);

                    if (hitVictim.Armor > 0 && !hitVictim.PhotonView.isMine)
                        Dispatcher.Send(
                            id:     EventId.TankTakesDamage,
                            info:   new EventInfo_U(
                                        hitVictim.data.playerId,
                                        hitVictim.CalcDamage(Damage),
                                        ownerId,
                                        (int)shellType,
                                        hit.point));

                    if (!hitVictim.PhotonView.isMine || !hitVictim.IsBot)
                        hitVictim.Armor -= Damage;
                }

                if (!victimIsMe)
                {
                    Explosion(position: hit.point, hitsVehicle: true);
                    return;
                }
            }
        }
        else
        {
            if (Physics.SphereCast(
                    /* origin:      */  transform.position,
                    /* radius:      */  deltaPos * RADIUS_RATIO,
                    /* direction:   */  transform.forward,
                    /* hitInfo:     */  out hit,
                    /* maxDistance: */  deltaPos,
                    /* layerMask:   */  hitMask) ||
                isCloseToTarget)
            {
                Explosion(
                    position:       transform.position,
                    hitsVehicle:    MiscTools.CheckIfLayerInMask(hitMask, Layer.Key.Player) ||
                                    MiscTools.CheckIfLayerInMask(hitMask, Layer.Key.Enemy));
            }
        }

        Quaternion targetRotation = Quaternion.LookRotation(targetPosition - transform.position);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, currentRotationSpeed * Time.deltaTime);

        transform.Translate(transform.forward * deltaPos, Space.World);

        previousDistanceToTarget = currentDistanceToTarget;

        flightDistance += deltaPos;
    }

    #if UNITY_EDITOR
    protected override void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(center: transform.position, radius: deltaPos * RADIUS_RATIO);
    }
    #endif

    public override void Activate(
        VehicleController       owner,
        int                     damage,
        int                     hitMask,
        int                     victimId  = BattleController.DEFAULT_TARGET_ID,
        ShellType               shellType = ShellType.Usual)
    {
        ownerId = owner.data.playerId;
        Damage = damage;
        this.hitMask = hitMask;
        this.victimId = victimId;
        this.shellType = shellType;

        isMisguided = false;

        gameObject.SetActive(true);

        currentSpeed = OwnerSpeed;

        if (IsLocal && !BotOwns)
            Dispatcher.Send(EventId.MyTankShots, new EventInfo_I((int)shellType));
    }

    private void OnShellStateChanged(EventId id, EventInfo ei)
    {
        EventInfo_BIIII info = (EventInfo_BIIII)ei;

        if (info.int2 == victimId &&
            (ShellType)info.int3 == ShellType.IRCM &&
            info.bool1)
        {
            isMisguided = true;
            victimIRCM = SelectFirstShellOrDefault<IRCM>(victimId);
        }
    }
}
