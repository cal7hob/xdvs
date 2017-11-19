using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class VehicleShopItemCell : ShopItemCell
{
    public GameObject sprDoubleExp;
    [SerializeField] private ActivatedUpDownButton comingSoonActivationScript;
    [SerializeField] private List<GameObject> objectsForTankGroups;//Пока не используется, сделали иконки по типам танков, раскрашенные по группам
    [SerializeField] private Color[] colorsForGroups;
    //[SerializeField] private Color colorForGroup_2 = Color.white;
    //[SerializeField] private Color colorForGroup_3 = Color.white;
    //[SerializeField] private Color colorForGroup_4 = Color.white;
    //[SerializeField] private Color colorForGroup_5 = Color.white;

    private VehicleUpgrades upgrades;

    private const string BUTTON_NAME_PREFIX = "btnVehicle";

    public UserVehicle UserVehicle { get; private set; }

    protected override string ButtonNamePrefix
    {
        get { return BUTTON_NAME_PREFIX; }
    }

    protected override Object RelatedShopWindow
    {
        get { return ShopManager.Instance.vehicleShop; }
    }

    public override void Set<TShopItemCell>(IShopItem shopItem, bool isLastItem)
    {
        base.Set<VehicleShopItemCell>(shopItem, isLastItem);

        VehicleInfo info = (VehicleInfo)shopItem;
        upgrades
            = ProfileInfo.vehicleUpgrades.ContainsKey(info.id)
                ? ProfileInfo.vehicleUpgrades[info.id]
                : null;

        HangarVehicle hangarVehicle = HangarVehiclesHolder.GetByIdOrDefault(info.id);

        UserVehicle = new UserVehicle(info, hangarVehicle, upgrades);

        if (UserVehicle.Upgrades.OwnedCamouflages.Any())
            UserVehicle.TryOnCamouflage(
                PatternPool.Instance.GetItemById(UserVehicle.Upgrades.CamouflageId));

        if (UserVehicle.Upgrades.OwnedDecals.Any())
            UserVehicle.TryOnDecal(
                DecalPool.Instance.GetItemById(UserVehicle.Upgrades.DecalId));

        sprDoubleExp.SetActive(ProfileInfo.doubleExpVehicles.Contains(info.id));

        if (sprImage && !info.isComingSoon)
            sprImage.SetSprite(hangarVehicle.TechnicalName);

        if (sprLockedImage != null)
            sprLockedImage.SetSprite(hangarVehicle.TechnicalName);

        if (comingSoonActivationScript)
            comingSoonActivationScript.Activated = info.isComingSoon;

        if (objectsForTankGroups != null)
            for (int i = 0; i < objectsForTankGroups.Count; i++)
                if (objectsForTankGroups[i] != null)
                    objectsForTankGroups[i].SetActive(info.vehicleGroup > objectsForTankGroups.Count  ? false : (info.vehicleGroup - 1) == i);//С проверкой на номер группы > 5
    }
}
