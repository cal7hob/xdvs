using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Shop : MonoBehaviour
{
    [Flags]
    public enum ItemType
    {
        None    = 0,
        Vehicle = 1 << 0,
        Module  = 1 << 1,
        Pattern = 1 << 2,
        Decal   = 1 << 3,
        All     = Vehicle | Module | Pattern | Decal
    }

    void Awake()
    {
        Messenger.Subscribe(EventId.ShopInfoLoadedFromServer, ShopInfoLoadedFromServer);
    }

    void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.ShopInfoLoadedFromServer, ShopInfoLoadedFromServer);
    }

    public static UserVehicle CurrentVehicle { get; set; }

    public static UserVehicle VehicleInView { get; set; }

    public static UserVehicle GetVehicle(int id)
    {
        return VehicleShop.Selectors[id].UserVehicle;
    }

    private static void ShopInfoLoadedFromServer(EventId id, EventInfo info)
    {
        if (VehicleShop.Instance == null || GUIPager.ActivePage != VehicleShop.Instance.GuiPageName)
            VehicleShop.ForcedFillPanel();
        else
            Shop.CurrentVehicle
                = VehicleShop.Selectors.ContainsKey(ProfileInfo.CurrentVehicle)
                    ? VehicleShop.Selectors[ProfileInfo.CurrentVehicle].UserVehicle
                    : null;

        HangarController.Instance.ShowUserInfo();
    }
}

public abstract class Shop<TShopItemCell> : MonoBehaviour
    where TShopItemCell : ShopItemCell
{
    public TShopItemCell shopItemCellPrefab;

    private float scrollPosition = -1;
    private tk2dUIToggleControl prevActiveToggleControl;

    public static Dictionary<int, TShopItemCell> Selectors { get; protected set; }

    public abstract string GuiPageName { get; }

    public abstract string OnToggleMethodName { get; }

    public tk2dUIScrollableArea ScrollableArea { get { return GetComponent<tk2dUIScrollableArea>(); } }

    public bool IsInShop { get { return GUIPager.ActivePage == GuiPageName; } }

    protected abstract Shop.ItemType ComparingMode { get; }

    protected abstract IShopItem[] ShopItems { get; }

    protected virtual AudioClip SelectSound { get { return HangarController.Instance.tankSelectSound; } }

    protected MeshFilter VehiclePanelBackground { get { return HangarController.Instance.bottomGuiPanel; } }

    protected LabelLocalizationAgent InaccessibleBoxLabel { get { return HangarController.Instance.InaccessibleBoxLabel; } }

    protected HangarBuyingBox BuyingBox { get { return HangarController.Instance.BuyingBox; } }

    protected HangarSaleBuyingBox SaleBuyingBox { get { return HangarController.Instance.saleBuyingBox; } }

    protected HangarRentBox RentBox { get { return HangarController.Instance.rentBox; } }

    protected HangarSaleRentBox SaleRentBox { get { return HangarController.Instance.saleRentBox; } }

    protected HangarRentingBox RentingBox { get { return HangarController.Instance.rentingBox; } }

    protected HangarRentedBox RentedBox { get { return HangarController.Instance.rentedBox; } }

    protected HangarDeliverBox DeliverBox { get { return HangarController.Instance.deliverBox; } }

    protected virtual void OnEnable()
    {
        FillPanel();
        HangarController.Instance.RecalcMaxStats(Shop.CurrentVehicle.Info.vehicleType, ComparingMode);
    }

    protected virtual void OnDisable() { }

    protected virtual void Update()
    {
        if (!GameData.timeGettingError)
            scrollPosition = GUIController.ScrollPanel(ScrollableArea, scrollPosition);
    }

    protected virtual void FillPanel()
    {
        IShopItem[] shopItems = ShopItems;

        Selectors = new Dictionary<int, TShopItemCell>(shopItems.Length);

        ScrollableArea.contentContainer.transform.DestroyChildren();

        ShopItemCell.ResetCellOffset();

        foreach (IShopItem shopItem in shopItems)
        {
            if (shopItem.HideCondition)
                continue;

            TShopItemCell shopItemCell = Instantiate(shopItemCellPrefab);
            shopItemCell.Set<TShopItemCell>(shopItem, shopItem == shopItems.Last());

            Selectors[shopItem.Id] = shopItemCell;
        }

        var itemLayout = shopItemCellPrefab.GetComponent<tk2dUILayout>();
        var itemWidth = (itemLayout.GetMaxBounds() - itemLayout.GetMinBounds()).x;

        // itemWidth потому что левая половина первого элемента и правая половина последнего
        ScrollableArea.ContentLength += ScrollableArea.VisibleAreaLength / 2 - itemWidth;

        UpdateKitsShopSelectorStickers();
    }

    protected void UpdateKitsShopSelectorStickers()
    {
        if (this is PatternShop)
        {
            Messenger.Send(EventId.PatternShopFilled, null);
        }
        else if (this is DecalShop)
        {
            Messenger.Send(EventId.DecalShopFilled, null);
        }
    }

    protected void SelectToggle(tk2dUIToggleControl control, bool immediately, out bool allowToPlaySound)
    {
        allowToPlaySound = true;
        control.IsOn = true;

        if (prevActiveToggleControl != null && prevActiveToggleControl != control)
            prevActiveToggleControl.IsOn = false;
        else
            allowToPlaySound = false;

        prevActiveToggleControl = control;

        var layout = control.GetComponent<tk2dUILayout>();
        
        var w = (layout.GetMaxBounds() - layout.GetMinBounds()).x;

        scrollPosition
            = Mathf.Abs(control.transform.localPosition.x + w / 2 - GUIController.halfScreenWidth) /
              (ScrollableArea.ContentLength - GUIController.halfScreenWidth * 2);

        if (immediately)
            scrollPosition += 2;
    }
}
