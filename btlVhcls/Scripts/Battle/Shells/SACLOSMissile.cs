using UnityEngine;

public class SACLOSMissile : Shell
{
    [Header("Настройки ракеты")]
    public float minSpeed = 20.0f;
    public float maxSpeed = 50.0f;
    public float rotationMinSpeed = 0.0f;
    public float rotationMaxSpeed = 200.0f;
    public float accelerationDistance = 2.5f;

    private const float HITTING_DISTANCE = 0.5f;
    private const float MIN_DISTANCE_TO_TARGET = 30.0f;

    private VehicleController target;
    private bool isHitTarget;
    private bool targetIsAvailable;
    private bool isLockedTargetAvailability;
    private float flightProgress;
    private Vector3 initialPosition;
    private Vector3 initialTargetPosition;
    private Vector3 lastTargetPosition;
    private RaycastHit hit;

    private Vector3 TargetPosition
    {
        get
        {
            if (target != null)
                lastTargetPosition = target.transform.TransformPoint(aimPointLocalToTarget);

            return lastTargetPosition;
        }
    }

    protected override void Update()
    {
        if (isExploded)
            return;

        Move();

        bool isHitSomething = CheckHitting(out isHitTarget, out hit);

        if (isHitTarget)
            SendDamage();

        if (isHitSomething || CheckOutOfMap())
        {
            Explosion(
                position:       transform.position,
                hitsVehicle:    isHitTarget,
                victim:         target);
        }
    }

    #if UNITY_EDITOR
    protected override void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(center: transform.position, radius: HITTING_DISTANCE);
    }
    #endif

    public override void Activate(VehicleController owner, int damage, int hitMask, int victimId = BattleController.DEFAULT_TARGET_ID, GunShellInfo.ShellType shellType = GunShellInfo.ShellType.Usual)
    {
        ownerId = owner.data.playerId;
        Damage = damage;
        this.victimId = victimId;
        this.shellType = shellType;
        this.hitMask = MiscTools.ExcludeLayerFromMask(owner.HitMask, owner.OwnLayer);
        target = BattleController.allVehicles.GetValueOrDefault(victimId);

        isLockedTargetAvailability = false;

        initialPosition = transform.position;
        SetTargetPosition();

        isExploded = false;

        gameObject.SetActive(true);

        if (IsLocal && !BotOwns)
            Dispatcher.Send(EventId.MyTankShots, new EventInfo_I((int)shellType));
    }

    private void SetTargetPosition()
    {
        if (target == null)
        {
            Debug.LogError("Cannot guide missile. Target is null!");

            Explosion(
                position:       transform.position,
                hitsVehicle:    false,
                victim:         null);

            return;
        }

        initialTargetPosition = TargetPosition;
    }

    private void SendDamage()
    {
        if (!IsLocal || target == null)
            return;

        int playerId = target.data.playerId;

        Dispatcher.Send(
            id:         EventId.TankTakesDamage,
            info:       new EventInfo_U(
                            /* victimId */      playerId,
                            /* damage */        target.CalcDamage(Damage),
                            /* attackerId */    ownerId,
                            /* shellType */     shellType,
                            /* hits */          1,
                            /* hitPosition */   Vector3.zero),
            target:     target.IsBot ? Dispatcher.EventTargetType.ToMaster : Dispatcher.EventTargetType.ToSpecific,
            specificId: playerId);
    }

    private void Move()
    {
        if (gameObject == null)
            return;

        Vector3 targetPosition = TargetPosition;
        Vector3 targetDirection = (targetPosition - transform.position).normalized;

        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

        flightProgress = Mathf.Clamp01(flightDistance / distanceToTarget);

        float rotationSpeed = Mathf.Lerp(rotationMinSpeed, rotationMaxSpeed, flightProgress);
        float speed = Mathf.Lerp(minSpeed, maxSpeed, flightProgress);
        bool targetIsFarEnough = Vector3.Distance(initialPosition, initialTargetPosition) > MIN_DISTANCE_TO_TARGET;

        if (!isLockedTargetAvailability)
            targetIsAvailable = target != null && target.IsAvailable;

        if (!targetIsAvailable)
            isLockedTargetAvailability = true;

        if (flightDistance < accelerationDistance && targetIsFarEnough)
            targetDirection = transform.forward;

        Vector3 translation = Vector3.forward * (OwnerSpeed + speed) * Time.deltaTime;
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

        if (!targetIsAvailable)
            rotationSpeed = 0;

        Quaternion resultRotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // Поворачивать сразу, если цель слишком близко.
        if (!targetIsFarEnough && targetIsAvailable)
            resultRotation = targetRotation;

        Vector3 translationToWorld = transform.TransformVector(translation);
        float distanceAfterTranslation = Vector3.Distance(transform.position + translationToWorld, targetPosition);

        // Подогнать ракету, куда нужно, если она собирается проскочить.
        if (distanceToTarget < distanceAfterTranslation && targetIsAvailable)
            translation = transform.InverseTransformVector(targetDirection * distanceToTarget);

        transform.rotation = resultRotation;
        transform.Translate(translation, Space.Self);

        flightDistance += translation.magnitude;
    }

    private bool CheckHitting(out bool hitTarget, out RaycastHit hit)
    {
        hitTarget = false;
        hit = new RaycastHit();

        bool hitSomething = Physics.CheckSphere(transform.position, HITTING_DISTANCE, hitMask);

        if (!hitSomething)
            return false;

        Collider[] colliders = Physics.OverlapSphere(transform.position, HITTING_DISTANCE, hitMask);

        foreach (Collider collider in colliders)
        {
            if (hitTarget)
                break;

            VehicleController vehicleController = collider.GetComponentInParent<VehicleController>();

            if (vehicleController != null && vehicleController == target)
                hitTarget = true;
        }

        if (hitTarget)
        {
            float rayLength = 5.0f;
            Vector3 rayDirection = transform.forward;
            Vector3 rayStart = transform.position;

            Physics.Raycast(
                /* ray:         */  new Ray(rayStart, rayDirection),
                /* hitInfo:     */  out hit,
                /* maxDistance: */  rayLength,
                /* layerMask:   */  hitMask);
        }

        return true;
    }

    private bool CheckOutOfMap()
    {
        return Vector3.Distance(transform.position, TargetPosition) > maxDistance;
    }
}
