using System;

public class VehiclePool : ShopItemPool<VehicleInfo>
{
    public override BodykitInEditor[] ReferencedBodykits
    {
        get { return null; }
    }

    protected override VehicleInfo[] GetItems()
    {
        HangarVehicle[] hangarVehicles = HangarVehiclesHolder.HangarVehicles;

        HelpTools.ImportComponentsVehicleInfo(hangarVehicles, GameData.vehiclesDataStorage, "tanks");

        VehicleInfo[] infos = new VehicleInfo[hangarVehicles.Length];

        for (int index = 0; index < hangarVehicles.Length; index++)
        {
            var hangarVehicle = hangarVehicles[index];
            var info = hangarVehicle.Info;

            infos[index] = info;
        }

        Array.Sort(
            array:      infos,
            comparison: (first, second) => first.position > second.position
                            ? 1
                            : first.position == second.position ? 0 : -1);

        return infos;
    }
}
