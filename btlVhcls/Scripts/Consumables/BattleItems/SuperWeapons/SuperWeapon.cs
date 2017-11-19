using UnityEngine;

public class SuperWeapon : BattleItem
{
    protected VehicleController target;
    protected Vector3 aimPointLocalToTarget;

    public override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);

        int targetId = (int)photonView.instantiationData[2];
        BattleController.allVehicles.TryGetValue(targetId, out target);

        aimPointLocalToTarget = (Vector3)photonView.instantiationData[3];
    }
}
