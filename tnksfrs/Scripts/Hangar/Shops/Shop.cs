using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using XD;

public class Shop : MonoBehaviour
{
    //public delegate void VehicleInViewEvent(UserVehicle VehicleInView);
    public delegate void VehicleInViewEvent(IUnitHangar VehicleInView);
    public static event VehicleInViewEvent  OnVehicleInViewEvent;
    private static IUnitHangar              vehicleInView;

    public static IUnitHangar CurrentVehicle
    {
        get;
        set;
    }

    public static IUnitHangar VehicleInView
    {
        get
        {
            return vehicleInView;
        }
        set
        {
            vehicleInView = value;
            if (OnVehicleInViewEvent != null)
            {
                OnVehicleInViewEvent(vehicleInView);
            }
        }
    }

    private void Awake()
    {
        Dispatcher.Subscribe(EventId.ShopInfoLoadedFromServer, ShopInfoLoadedFromServer);
    }

    private void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.ShopInfoLoadedFromServer, ShopInfoLoadedFromServer);
    }
    
    private static void ShopInfoLoadedFromServer(EventId id, EventInfo info)
    {
     
    }
}

public abstract class Shop<TShopItemCell> : MonoBehaviour
    where TShopItemCell : ShopItemCell
{
    public TShopItemCell shopItemCellPrefab;

    private float scrollPosition = -1;

    public static Dictionary<int, TShopItemCell> Selectors { get; protected set; }

    public abstract string GuiPageName { get; }

    public abstract string OnToggleMethodName { get; }

    public bool IsInShop { get { return GUIPager.ActivePage == GuiPageName; } }

    protected abstract XD.ShopItemType ComparingMode { get; }

    protected abstract IShopItem[] ShopItems { get; }

    protected virtual AudioClip SelectSound { get { return HangarController.Instance.tankSelectSound; } }

    protected MeshFilter VehiclePanelBackground { get { return HangarController.Instance.bottomGuiPanel; } }

    protected virtual void OnEnable()
    {
        FillPanel();
        HangarController.Instance.RecalcMaxStats(ComparingMode);
    }

    protected virtual void OnDisable() { }

    protected virtual void Update()
    {
    }

    protected virtual void FillPanel()
    {
        IShopItem[] shopItems = ShopItems;

        Selectors = new Dictionary<int, TShopItemCell>(shopItems.Length);

        ShopItemCell.ResetCellOffset();

        foreach (IShopItem shopItem in shopItems)
        {
            if (shopItem.HideCondition)
                continue;

            TShopItemCell shopItemCell = Instantiate(shopItemCellPrefab);
            shopItemCell.Set<TShopItemCell>(shopItem);

            Selectors[shopItem.Id] = shopItemCell;
        }

        UpdateKitsShopSelectorStickers();
    }

    protected void UpdateKitsShopSelectorStickers()
    {
    }

    /*protected void SelectToggle(tk2dUIToggleControl control, bool immediately, out bool allowToPlaySound)
    {
        allowToPlaySound = true;
        control.IsOn = true;

        if (immediately)
            scrollPosition += 2;
    }*/
}
