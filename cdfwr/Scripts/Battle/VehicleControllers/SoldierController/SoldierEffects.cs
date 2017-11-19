using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pool;

public partial class SoldierController
{
    public void PlayHitEffect(Vector3 point, Vector3 normal)
    {
        var effect = PoolManager.GetObject<ParticleEffect>(terrainHitPrefabPath);
        if (effect != null)
        {
            effect.transform.position = point + normal * 0.1f;
            effect.transform.rotation = Quaternion.LookRotation(normal);
        }
    }

    public void PlayShotEffect( Vector3 aim)
    {
        if (shootEffectPoints == null || shootEffectPoints.Count == 0)
        {
            GetShotEffect(aim);
        }
        else
        {
            foreach (Transform point in shootEffectPoints)
            {
                GetShotEffect(aim);
            }
        }
    }
    private void GetShotEffect(Vector3 aim) 
    {
        var shotEffect = PoolManager.GetObject<ParticleEffect>(shotPrefabPath);
        if (shotEffect != null)
        {
            shotEffect.transform.position = cannonEnd.transform.position;
            shotEffect.transform.LookAt(aim) ;
        }
    }

    public void PlayVictimHitEffect(Vector3 point, Vector3 normal)
    {
        var effect = PoolManager.GetObject<ParticleEffect>(hitPrefabPath);
        if (effect != null)
        {
            effect.transform.position = point + normal*0.1f;
            effect.transform.rotation = Quaternion.LookRotation(normal);
        }
    }
}
