using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [Header("Магазины")]

    public VehicleShop vehicleShop;
    public PatternShop patternShop;
    public DecalShop decalShop;

    [Header("Костыли")]

    public VehicleShopItemCell vehicleShopItemCellPrefab;
    public PatternShopItemCell patternShopItemCellPrefab;
    public DecalShopItemCell decalShopItemCellPrefab;

    public static ShopManager Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        Instance = null;
    }

    public void Load()
    {
        if (GameData.vehiclesDataStorage == null)
        {
            return;
        }

        HelpTools.ImportComponentsModuleInfos(TankModuleInfos.Instance, GameData.vehiclesDataStorage, "modules");

        VehicleShop.ForcedFillPanel();
        PatternShop.ForcedFillPanel();
        DecalShop.ForcedFillPanel();

        ModuleShop.Instance.CheckIfModuleUpgradePossible();
    }
}
