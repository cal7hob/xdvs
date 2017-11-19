using UnityEngine;
using XDevs.LiteralKeys;

public class CrashableVehicleAircraft : CrashableVehicleBase
{
	public new Animation animation;
    public Transform animatedBody;
    public Transform damageEffect;
    public GameObject explosionPrefab;

    private const float ANIMATION_SPEED = 1.0f;
    private const float ANIMATION_LENGTH = 0.95f;
    private const float EXPLOSION_DELAY = 0.7f;
    private const float CAMERA_INERTION_RATIO = 1.0f;

    private CrashTransformVehicle crashObject;
    private bool isCrashStarted;
    private bool isExploded;
    private bool isAnimatedBodyStored;
    private float lastVehicleSpeed;
    private float animationStartTime;
    private Vector3 initialLocalPosition;
    private Vector3 damageEffectInitialLocalPosition;
    private Vector3 damageEffectInitialScale;
    private Vector3 animatedBodyInitialLocalPosition;
    private Vector3 animatedBodyInitialScale;
    private Quaternion initialLocalRotation;
    private Quaternion damageEffectInitialLocalRotation;
    private Quaternion animatedBodyInitialLocalRotation;
    private Transform initialParent;
    private Transform damageEffectInitialParent;
    private Renderer[] effectsRenderers;

    private bool IsPlayingAnimation
    {
        get { return Time.time - animationStartTime < (animation.clip.length * ANIMATION_LENGTH) / animation[animation.clip.name].speed; }
    }

    private bool IsRequireExplosion
    {
        get { return !(Time.time - animationStartTime < (animation.clip.length * EXPLOSION_DELAY) / animation[animation.clip.name].speed); }
    }

    protected override void Awake()
    {
        base.Awake();

        crashObject = GetComponentInChildren<CrashTransformVehicle>(true);

        crashObject.Init(vehicleController);

        initialLocalPosition = crashObject.transform.localPosition;
        initialLocalRotation = crashObject.transform.localRotation;
        initialParent = crashObject.transform.parent;

        StoreDamageEffectInitials();

        Dispatcher.Subscribe(EventId.TankRespawned, OnTankRespawned);
        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Dispatcher.Unsubscribe(EventId.TankRespawned, OnTankRespawned);
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
    }

    void FixedUpdate()
    {
        if (!isCrashStarted)
            return;

        // Костыль для привязывания взорванного самолёта к краш объекту, чтобы при помирании на высокой 
        // скорости камера была близко к крашу.
        if (vehicleController.PhotonView.isMine)
        {
            vehicleController.transform.position
                = Vector3.MoveTowards(
                    current:            vehicleController.transform.position,
                    target:             animatedBody.position,
                    maxDistanceDelta:   lastVehicleSpeed * CAMERA_INERTION_RATIO * Time.fixedDeltaTime);
        }

        if (IsRequireExplosion && !isExploded)
        {
            EffectPoolDispatcher.GetFromPool(
                _effect:    explosionPrefab,
                _position:  animatedBody.position,
                _rotation:  Quaternion.identity);

            isExploded = true;
        }

        if (!IsPlayingAnimation)
        {
            Restore();

            if (vehicleController.data.playerId == BattleController.MyPlayerId)
                Dispatcher.Send(EventId.DeathAnimationDone, new EventInfo_SimpleEvent());
        }
    }

    protected override void OnTankKilled(EventId id, EventInfo ei)
    {
        int victimId = ((EventInfo_III)ei).int1;

        if (victimId == vehicleController.data.playerId)
            Crash();
    }

    private void OnTankRespawned(EventId id, EventInfo ei)
    {
        int victimId = ((EventInfo_I)ei).int1;

        if (victimId == vehicleController.data.playerId)
            Restore();
    }

    private void OnMainTankAppeared(EventId id, EventInfo ei)
    {
        Restore();
    }

    private void Crash()
    {
        MoveDamageEffect();

        crashObject.transform.parent = null;
        crashObject.gameObject.SetActive(true);

        Transform[] children = crashObject.gameObject.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
            child.gameObject.layer = LayerMask.NameToLayer(Layer.Items[Layer.Key.Default]);

        foreach (MeshRenderer meshRenderer in crashObject.gameObject.GetComponentsInChildren<MeshRenderer>(true))
            meshRenderer.enabled = true;

        animation[animation.clip.name].speed = ANIMATION_SPEED;

        animation.Play();

        animationStartTime = Time.time;

        lastVehicleSpeed = vehicleController.CurrentSpeed;

        isCrashStarted = true;
    }

    private void Restore()
    {
        isCrashStarted = false;
        isExploded = false;

        animation.Stop();

        crashObject.transform.parent = initialParent;
        crashObject.transform.localPosition = initialLocalPosition;
        crashObject.transform.localRotation = initialLocalRotation;
        crashObject.gameObject.SetActive(false);

        RestoreDamageEffect();
    }

    private void StoreDamageEffectInitials()
    {
        effectsRenderers = crashObject.GetComponentsInChildren<Renderer>(true);

        damageEffectInitialLocalPosition = damageEffect.localPosition;
        damageEffectInitialLocalRotation = damageEffect.localRotation;
        damageEffectInitialScale = damageEffect.localScale;
        
        damageEffectInitialParent = damageEffect.parent;
    }

    private void MoveDamageEffect()
    {
        // Костыль, возвращающий анимированную часть обратно. Animation.Sample() и т.д. не дают нужного результата.
        if (!isAnimatedBodyStored)
        {
            animatedBodyInitialLocalPosition = animatedBody.localPosition;
            animatedBodyInitialLocalRotation = animatedBody.localRotation;
            animatedBodyInitialScale = animatedBody.localScale;

            isAnimatedBodyStored = true;
        }

        animatedBody.localPosition = animatedBodyInitialLocalPosition;
        animatedBody.localRotation = animatedBodyInitialLocalRotation;
        animatedBody.localScale = animatedBodyInitialScale;

        damageEffect.parent = animatedBody;

        // Костылик. Рендереры этого эффекта почему-то оказываются отключенными.
        foreach (Renderer damageEffectRenderer in effectsRenderers)
            damageEffectRenderer.enabled = true;
    }

    private void RestoreDamageEffect()
    {
        damageEffect.parent = damageEffectInitialParent;

        damageEffect.localPosition = damageEffectInitialLocalPosition;
        damageEffect.localRotation = damageEffectInitialLocalRotation;
        damageEffect.localScale = damageEffectInitialScale;

        damageEffect.gameObject.layer = damageEffectInitialParent.root.gameObject.layer;

        foreach (Transform child in damageEffect)
            child.gameObject.layer = damageEffectInitialParent.root.gameObject.layer;

        foreach (Renderer damageEffectRenderer in effectsRenderers)
            damageEffectRenderer.enabled = false;
    }
}
