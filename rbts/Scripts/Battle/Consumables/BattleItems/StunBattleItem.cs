using System.Collections;
using System.Collections.Generic;
using Pool;
using UnityEngine;

public class StunBattleItem : BattleItem
{
    [SerializeField] private FXInfo stunRayFX;
    private StunRay stunRay;

    protected override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);

        stunRay = PoolManager.GetObject<StunRay>(stunRayFX.GetResourcePath(owner.IsMain), transform.position, transform.rotation);
        stunRay.Activate(owner.AimingPoint, itemTarget.transform);
        AudioDispatcher.PlayClipAtPosition(GameSettings.Instance.StunRaySound.GetObject<AudioClip>(), owner.transform);

        if (owner.PhotonView.isMine)
        {
            StartCoroutine(DestroyRoutine());
        }
    }

    private void StunTarget()
    {
        if (!itemTarget.IsAvailable)
        {
            return;
        }

        VehicleEffectData effectData = new VehicleEffectData(VehicleEffect.ParameterType.Stun, VehicleEffect.ModifierType.Fixed, 1f, consumableInfo.duration);
        itemTarget.RequestEffect(effectData);
    }

    private IEnumerator DestroyRoutine()
    {
        yield return new WaitForSeconds(0.15f);
        stunRay.ReturnObject();
        if (owner.PhotonView.isMine)
        {
            StunTarget();
            yield return new WaitForSeconds(1f); // Чтобы высокопинговые клиенты смогли всё-таки увидеть эффект выстрела
            PhotonNetwork.Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        stunRay.ReturnObject();
    }
}
