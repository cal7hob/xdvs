public class BattleItem : Photon.MonoBehaviour
{
    protected VehicleController owner;
    protected ConsumableInfo consumableInfo;
    protected double instantiationTime;

    public virtual void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        instantiationTime = info.timestamp;

        int consumableId = (int)photonView.instantiationData[1];
        consumableInfo = GameData.consumableInfos[consumableId];

        int ownerId = (int) photonView.instantiationData[0];
        BattleController.allVehicles.TryGetValue(ownerId, out owner);
    }

    protected VehicleController SelectPowerContextVehicle(VehicleController target)
    {
        switch (consumableInfo.powerContext)
        {
            case DamageCalcContext.Target:
                return target;
            case DamageCalcContext.Owner:
                return owner;
            default:
                return null;
        }
    }

    protected int CalcPower(VehicleController target = null)
    {
        if (consumableInfo.powerModifier == VehicleEffect.ModifierType.Fixed)
            return (int)consumableInfo.powerValue;

        float result = 0;
        VehicleController contextVehicle = SelectPowerContextVehicle(target);

        if (contextVehicle == null)
            return (int)result;

        float sourceValue = contextVehicle.GetParameterForCalc(consumableInfo.powerParameter);

        if (consumableInfo.powerModifier == VehicleEffect.ModifierType.Sum)
            result = sourceValue + consumableInfo.powerValue;

        if (consumableInfo.powerModifier == VehicleEffect.ModifierType.Product)
            result = sourceValue * consumableInfo.powerValue;

        return (int)result;
    }
}