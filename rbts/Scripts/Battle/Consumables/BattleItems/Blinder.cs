using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blinder : BattleItem
{
    private Transform targetTransform;
    [SerializeField] private float speed;
    
    protected override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);
        targetTransform = itemTarget.transform;
        AudioDispatcher.PlayClipAtPosition(GameSettings.Instance.BlindingRaySound.GetObject<AudioClip>(), transform.position);
    }

    private void Update()
    {
        transform.LookAt(targetTransform);
        transform.position = Vector3.MoveTowards(transform.position, targetTransform.position, speed * Time.deltaTime);

        if (Vector3.SqrMagnitude(targetTransform.position - transform.position) < 0.05f)
        {
            if (owner.PhotonView.isMine)
            {
                itemTarget.RequestEffect(new VehicleEffectData(VehicleEffect.ParameterType.Blindness, VehicleEffect.ModifierType.Fixed, 1f, consumableInfo.duration, -1));
                PhotonNetwork.Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}