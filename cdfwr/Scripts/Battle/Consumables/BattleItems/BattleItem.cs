using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleItem : Photon.MonoBehaviour
{
    protected VehicleController owner;
    protected ConsumableInfo consumableInfo;
    protected double instantiationTime;
    protected float ownerContextParameterValue;

    protected virtual void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        instantiationTime = info.timestamp;
        int consumableId = (int)photonView.instantiationData[1];
        consumableInfo = GameData.consumableInfos[consumableId];

        int ownerId = (int) photonView.instantiationData[0];
        BattleController.allVehicles.TryGetValue(ownerId, out owner);

        if (consumableInfo.powerContext == DamageCalcContext.Owner && owner != null)
            ownerContextParameterValue = owner.GetParameterForCalc(consumableInfo.powerParameter);
    }

    protected VehicleController SelectPowerContextVeh(VehicleController vehicle)
    {
        switch (consumableInfo.powerContext)
        {
            case DamageCalcContext.Target:
                return vehicle;
            default:
                return owner;
        }
    }
}