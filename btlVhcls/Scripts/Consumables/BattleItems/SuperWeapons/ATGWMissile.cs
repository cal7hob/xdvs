using UnityEngine;

public class ATGWMissile : SuperWeapon
{
    [Header("Ссылки")]
    public GameObject defaultExplosionEffect;
    public GameObject targetExplosionEffect;
    public GameObject shotEffect;
    public AudioClip[] shotSounds;
    public AudioClip[] explosionSounds;

    [Header("Физика")]
    public float minSpeed = 20.0f;
    public float maxSpeed = 50.0f;
    public float rotationMinSpeed = 0.0f;
    public float rotationMaxSpeed = 200.0f;
    public float accelerationDistance = 2.5f;

    private const float HITTING_DISTANCE = 0.25f; // TODO: возможно, стоит заменить на consumableInfo.radius.
    private const float OUT_OF_MAP_DISTANCE = 1500.0f;
    private const float MIN_DISTANCE_TO_TARGET = 30.0f;
    private const float DESTROY_DURATION = 10.0f;
    private const GunShellInfo.ShellType SHELL_TYPE = GunShellInfo.ShellType.Missile_ATGW;

    private bool isHitTarget;
    private bool isExploded;
    private int hitMask;
    private float flightDistance;
    private float flightProgress;
    private Vector3 lastTargetPosition;
    private Vector3 initialPosition;
    private Vector3 initialTargetPosition;
    private Transform shotPoint;
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

    public override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);

        Dispatcher.Subscribe(EventId.TankDamageApplied, OnTankDamageApplied);

        SetPosition();
        SetTargetPosition();

        hitMask = MiscTools.ExcludeLayerFromMask(owner.HitMask, owner.OwnLayer);

        EffectPoolDispatcher.GetFromPool(
            _effect:        shotEffect,
            _position:      transform.position,
            _rotation:      transform.rotation,
            useEffectMover: true,
            moverTarget:    shotPoint);

        AudioDispatcher.PlayClipAtPosition(shotSounds.GetRandomItem(), transform.position);

        if (owner.IsMain)
            Dispatcher.Send(EventId.MyTankShots, new EventInfo_I((int)SHELL_TYPE));
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TankDamageApplied, OnTankDamageApplied);
    }

    void Update()
	{
        if (isExploded)
            return;

	    Move();

        bool isHitSomething = CheckHitting(out isHitTarget, out hit);

        if (isHitTarget)
            SendDamage();

        if (isHitSomething || CheckOutOfMap())
            Explode();
    }

    private void OnTankDamageApplied(EventId id, EventInfo ei)
    {
        if (!photonView.isMine)
            return;

        EventInfo_U info = (EventInfo_U)ei;

        int victimId = (int)info[0];
        int attackerId = (int)info[2];
        GunShellInfo.ShellType shellType = (GunShellInfo.ShellType)(int)info[3];

        if (shellType == GunShellInfo.ShellType.Missile_ATGW &&
            attackerId == owner.OwnerId &&
            victimId == target.OwnerId)
        {
            CancelInvoke();
            DelayedNetworkDestroying();
        }
    }

    private void SetPosition()
    {
        shotPoint = owner.GetShotPoint(this);

        transform.SetParent(shotPoint);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        transform.parent = null;

        initialPosition = transform.position;
    }

    private void SetTargetPosition()
    {
        if (target == null)
        {
            Debug.LogError("Cannot guide missile. Target is null!");
            Explode();
            return;
        }

        initialTargetPosition = TargetPosition;
    }

    private void SendDamage()
    {
        if (!photonView.isMine || target == null)
            return;

        int playerId = target.data.playerId;
        float power = CalcPower(target);

        Dispatcher.Send(
            id:         EventId.TankTakesDamage,
            info:       new EventInfo_U(
                            /* victimId */      playerId,
                            /* damage */        (int)power,
                            /* attackerId */    owner.data.playerId,
                            /* shellType */     SHELL_TYPE,
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
        bool targetIsAvailable = target != null && target.IsAvailable;

        if (flightDistance < accelerationDistance && targetIsFarEnough)
            targetDirection = transform.forward;

        Vector3 translation = Vector3.forward * speed * Time.deltaTime;
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

        if (!targetIsAvailable)
            rotationSpeed = 0;

        Quaternion resultRotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // Поворачивать сразу, если цель слишком близко.
        if (!targetIsFarEnough && targetIsAvailable)
            resultRotation = targetRotation;

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
        return Vector3.Distance(transform.position, TargetPosition) > OUT_OF_MAP_DISTANCE;
    }

    private void PlayExplosionEffects()
    {
        if (isHitTarget && hit.transform != null && hit.transform.IsChildOf(target.transform))
        {
            EffectPoolDispatcher.GetFromPool(
                _effect:        targetExplosionEffect,
                _position:      hit.point,
                _rotation:      Quaternion.LookRotation(hit.normal),
                useEffectMover: true,
                moverTarget:    hit.transform);
        }
        else if (hit.transform != null)
        {
            EffectPoolDispatcher.GetFromPool(defaultExplosionEffect, hit.point, Quaternion.LookRotation(hit.normal));
        }
        else
        {
            EffectPoolDispatcher.GetFromPool(defaultExplosionEffect, transform.position, transform.rotation);
        }

        AudioDispatcher.PlayClipAtPosition(explosionSounds.GetRandomItem(), transform.position);
    }

    private void Explode()
    {
        isExploded = true;

        foreach (Transform child in transform)
            child.gameObject.SetActive(false);

        PlayExplosionEffects();

        if (photonView.isMine)
            this.Invoke(DelayedNetworkDestroying, DESTROY_DURATION);
    }

    private void DelayedNetworkDestroying()
    {
        PhotonNetwork.Destroy(gameObject);
    }
}
