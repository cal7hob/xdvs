using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class HangarConsumableItem : MonoBehaviour, IItem
{
    public tk2dSlicedSprite sizeBg;//для определения размера итема

    [SerializeField] private tk2dTextMesh lblDescription;
    [SerializeField] private tk2dTextMesh lblCount;
    [SerializeField] private tk2dUIToggleControl checkBox_ForBattle;
    [SerializeField] private tk2dBaseSprite[] sprites;//специфичная для итема текстура
    [SerializeField] private PriceRenderer setPriceScript;
    [SerializeField] private ActivatedUpDownButton btnBuy;
    [SerializeField] private tk2dTextMesh lblLotSize;//Количество единиц расходки в паке
    [SerializeField] private GameObject vipIconWrapper;
    [SerializeField] private tk2dUIItem uiItem;
    [SerializeField] private tk2dUIItem btnBuyUIItem;

    private int ConsumableId { get; set; }
    private ConsumableInfo consumableInfo { get { return GameData.consumableInfos[ConsumableId]; } }

    public int Count
    {
        get
        {
            var count = ProfileInfo.HaveConsumable(ConsumableId) ? (int)ProfileInfo.consumableInventory[ConsumableId] : 0;

            if (ConsumablesPage.Instance.inventoryPanel.HasConsumable(ConsumableId))
                count -= ConsumablesPage.Instance.inventoryPanel.GetInventoryCellByConsumableId(ConsumableId).Count;

            return count;
        }
    }

    private void Awake()
    {
        Messenger.Subscribe(EventId.OnLanguageChange, OnLanguageChange);

        uiItem.OnClick += OnItemClickHandler;
        btnBuyUIItem.OnClick += OnBtnBuyClickHandler;
    }

    private void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.OnLanguageChange, OnLanguageChange);

        uiItem.OnClick -= OnItemClickHandler;
        btnBuyUIItem.OnClick -= OnBtnBuyClickHandler;
    }

    private void OnEnable()
    {
        UpdateComponents();
    }

    private void OnBtnBuyClickHandler()
    {
        if (!consumableInfo.IsEnabledByVipStatus)
        {
            Messenger.Send(EventId.VIPConsumableClicked, new EventInfo_I(ConsumableId));

            HangarController.Instance.NavigateToVipShop(
                showMessageBox: true,
                negativeCallback: () =>
                {
                    GUIPager.SetActivePage("ConsumablesPage");
                },
                positiveCallback: () =>
                {
                    GoogleAnalyticsDispatcher.StartEventChain(GAEvent.Category.VIPAccountBoughtAfterVIPConsumableClicked, ConsumableId);
                });

            return;
        }

        ConsumablesPage.Instance.BuyConsumable(ConsumableId,
            beforeRequest: () => { btnBuy.Activated = false; },
            afterResult: () => { btnBuy.Activated = true; UpdateComponents(); });
    }

    private void OnItemClickHandler()
    {
        if (!ProfileInfo.HaveConsumable(ConsumableId))
            return;

        Messenger.Send(EventId.ChangeConsumableInventoryState,
            new EventInfo_IB(ConsumableId, !checkBox_ForBattle.IsOn));
    }

    public void Initialize(object[] parameters)
    {
        ConsumableId = (int)parameters[0];

        name = string.Format("ID: {0}, {1}", ConsumableId, consumableInfo.name);

        UpdateComponents();

        checkBox_ForBattle.IsOn = false;
    }

    public void UpdateComponents()
    {
        lblCount.text = Count.ToString();
        //lblDescription.text = string.Format("id = {0}, sprite = {1}", ConsumableId, GameData.consumableInfos[ConsumableId].icon);
        lblDescription.text = consumableInfo.LocalizedDescription;
        setPriceScript.Price = consumableInfo.price.ToPrice();

        HelpTools.SetSpriteToAllSpritesInCollection(sprites, consumableInfo.GetIcon(true));

        vipIconWrapper.SetActive(consumableInfo.isVip);

        if (lblLotSize)
            lblLotSize.text = string.Format("{0} {1}", consumableInfo.countToBuy, Localizer.GetText("pieces"));

        if (consumableInfo.isHidden)
            btnBuy.Activated = false;
    }

    public void DestroySelf() { }

    public Vector2 GetSize()
    {
        return new Vector2(sizeBg.dimensions.x * sizeBg.scale.x, sizeBg.dimensions.y * sizeBg.scale.y);
    }

    private void OnLanguageChange(EventId evId, EventInfo ev)
    {
        UpdateComponents();
    }

    public bool IsForBattle
    {
        get
        {
            return checkBox_ForBattle.IsOn;
        }
        set
        {
            checkBox_ForBattle.IsOn = value;
        }
    }

    public string GetUniqId { get { return ConsumableId.ToString(); } }

    public tk2dUIItem MainUIItem { get { return uiItem; } }

    public Transform MainTransform { get { return transform; } }
}
