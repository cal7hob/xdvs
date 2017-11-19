using Pool;
using UnityEngine;
using VFX;

public abstract class CrashableObjectBase : MonoBehaviour
{
    [SerializeField, AssetPathGetter] protected string[] particlePrefabPaths;
    [SerializeField] protected Transform effectPoint;

    private bool isCrashed = false;
    protected const float SOUND_VOLUME_RATIO = 1.0f;

    void Start()
    {
        PreWarm();
    }

    private void PreWarm()
    {
        foreach (var prefabPath in particlePrefabPaths)
        {
            PoolManager.PreWarm<Effect>(prefabPath, 3);
        }
    } 

    protected virtual void CrashItself()
    {
        PlayEffects();    
    }

    public void Crash(Collider coll)
    {
        if (isCrashed)
        {
            return;
        }
        isCrashed = true;
        CrashItself();
    }

    protected void PlayEffects()
    {
        Vector3 effectPosition = transform.position;
        Quaternion effectRotation = transform.rotation;

        if (effectPoint != null)
        {
            effectPosition = effectPoint.position;
            effectRotation = effectPoint.rotation;
        }

        foreach (var particlePrefabPath in particlePrefabPaths)
        {
            if (string.IsNullOrEmpty(particlePrefabPath))
            {
                Debug.LogWarning("Missing effect prefab reference!");
                continue;
            }

            var effect = PoolManager.GetObject<Effect>(particlePrefabPath);
            effect.transform.position = effectPosition;
            effect.transform.rotation = effectRotation;
        }
    }
}
