using System.Collections;
using System.Collections.Generic;
using Pool;
using UnityEngine;

// Силовой щит вокруг транспорта
// Для идеального отображения должен иметь скейл и размеры (1; 1; 1).
public class ShieldFX : MonoBehaviour
{
    [System.Serializable]
    public class ShieldFXInfo
    {
        public FXInfo startFX;
        public FXInfo endFX;
    }

    private const float NORMAL_RADIUS = 5.9f;

    private ShieldFXInfo info;
    private PoolEffect startFX;
    private AudioClip enabledSound;
    private AudioClip disabledSound;
    private DPool<PoolEffect> endFXPool;

    private VehicleController owner;

    private void Awake()
    {
        owner = GetComponent<VehicleController>();
    }

    public void SetTakenDamageRatio(float damageRatio)
    {
        if (damageRatio < 0.01f)
        {
            return;
        }

        if (damageRatio > 0.66f)
        {
            info = GameSettings.Instance.LowShieldInfo;
        }
        else if (damageRatio > 0.33)
        {
            info = GameSettings.Instance.MedShieldInfo;
        }
        else
        {
            info = GameSettings.Instance.HighShieldInfo;
        }

        PoolManager.GetPoolAsync<PoolEffect>(info.startFX.GetResourcePath(owner.IsMain), 5, OnStartFXLoaded);
        PoolManager.GetPoolAsync<PoolEffect>(info.endFX.GetResourcePath(owner.IsMain), 5, OnEndFXLoaded);
    }

    private void OnStartFXLoaded(IPool commonPool)
    {
        DPool<PoolEffect> pool = (DPool<PoolEffect>) commonPool;
        PoolEffect fx = pool.GetObject(Vector3.zero, Quaternion.identity);
        startFX = PrepareShieldFX(fx);
        startFX.transform.SetParent(transform);
        startFX.gameObject.SetLayerRecursively(BattleController.DefaultLayer);
        AudioDispatcher.PlayClipAtPosition(GameSettings.Instance.ShieldEnabledSound.GetObject<AudioClip>(), owner.transform.position, owner.transform);
    }

    private void OnEndFXLoaded(IPool commonPool)
    {
        endFXPool = (DPool<PoolEffect>) commonPool;
    }

    private PoolEffect PrepareShieldFX(PoolEffect fx)
    {
        fx.transform.SetParent(transform);
        Bounds entBounds = owner.EntireBounds;
        fx.transform.position = new Vector3(entBounds.center.x, entBounds.min.y, entBounds.center.z);
        float radius = CalcColliderRadius(owner.EntireCollider);

        ParticleSystem fxPS = fx.GetComponentInChildren<ParticleSystem>();
        ParticleSystem.MainModule mainModule = fxPS.main;
        mainModule.startSizeMultiplier = radius / NORMAL_RADIUS * 1.1f; //Увеличиваем, чтобы щит не ложился "впритык"

        return fx;
    }

    public void Disactivate()
    {
        ShowDisactivation();
        Destroy(this);
    }

    private void ShowDisactivation()
    {
        startFX.Disactivate();
        startFX = null;

        if (endFXPool != null)
        {
            PoolEffect endFX = PrepareShieldFX(endFXPool.GetObject(transform.position, transform.rotation, transform));
            endFX.gameObject.SetLayerRecursively(BattleController.DefaultLayer);
            AudioDispatcher.PlayClipAtPosition(GameSettings.Instance.ShieldDisabledSound.GetObject<AudioClip>(), owner.transform.position, owner.transform);
        }
    }

    private static float CalcColliderRadius(Collider collider)
    {
        BoxCollider boxCollider = collider as BoxCollider;
        if (boxCollider != null)
        {
            Vector3 size = boxCollider.size;
            size.Scale(boxCollider.transform.lossyScale);
            Bounds bounds = new Bounds(Vector3.zero, size);
            Vector3 fxCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

            return Vector3.Distance(fxCenter, bounds.max);
        }
        
        SphereCollider sphereCollider = collider as SphereCollider;
        if (sphereCollider != null)
        {
            return sphereCollider.radius * 2f;
        }

        Debug.LogErrorFormat(collider.gameObject, "ShieldFX: unsupported entire collider's type ({0})", collider.GetType().Name);
        return 0f;
    }
}
