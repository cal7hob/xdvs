using UnityEngine;

public class ConsumableItem : ScrollableItem
{
    public GameObject GameObject { get; private set; }
    public Transform Transform { get; private set; }

    public tk2dSlicedSprite sizeBg;//для определения размера итема

    [SerializeField] private tk2dTextMesh lblDescription;
    [SerializeField] private tk2dTextMesh lblCount;
    [SerializeField]private tk2dTextMesh lblLotSize;
    [SerializeField] private tk2dUIToggleControl checkBox_ForBattle;
    [SerializeField] private tk2dBaseSprite[] sprites;//специфичная для итема текстура
    [SerializeField] private PriceRenderer setPriceScript;
    [SerializeField] private ActivatedUpDownButton btnBuy;
    [SerializeField] private tk2dUIItem uiItem;

    public int ConsumableId { get; private set; }
    public tk2dUIItem UiItem { get { return uiItem; } }

    public int Count { get { return (ProfileInfo.consumableInventory != null && ProfileInfo.consumableInventory.ContainsKey(ConsumableId)) ? (int)ProfileInfo.consumableInventory[ConsumableId] : 0; } }

    private void OnClick(tk2dUIItem btn)
    {
        switch (btn.name)
        {
            case "btnBuy":
                if (!GameData.consumableInfos[ConsumableId].IsEnabledByVipStatus)
                {
                    Dispatcher.Send(EventId.VipConsumableClicked, new EventInfo_I(ConsumableId));

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
                    () => { btnBuy.Activated = false; }, 
                    () => { btnBuy.Activated = true;
                    UpdateComponents();
                    AddConsumablesToEmpty();
                    MenuController.BuyingConsumablesSound();
                });
                break;
        }
    }

    public void AddConsumablesToEmpty()
    {
        if (ConsumablesPage.Instance.inventoryPanel.HasEmptyCell())
        {
            if (ProfileInfo.consumableInventory == null || !ProfileInfo.consumableInventory.ContainsKey(ConsumableId) || ProfileInfo.consumableInventory[ConsumableId] == 0)
                return;
            checkBox_ForBattle.IsOn = ConsumablesPage.Instance.inventoryPanel.Add(ConsumableId);
        }
    }
    private void OnItemClick(tk2dUIItem btn)
    {
        if (ProfileInfo.consumableInventory == null || !ProfileInfo.consumableInventory.ContainsKey(ConsumableId) || ProfileInfo.consumableInventory[ConsumableId] == 0)
            return;

        if (checkBox_ForBattle.IsOn)
            checkBox_ForBattle.IsOn = ConsumablesPage.Instance.inventoryPanel.Remove(ConsumableId);
        else
            checkBox_ForBattle.IsOn = ConsumablesPage.Instance.inventoryPanel.Add(ConsumableId);
    }

    public override void Initialize(params object[] parameters)
    {
      
        ConsumableId = (int)parameters[0];

        UpdateComponents();

        checkBox_ForBattle.IsOn = false;
    }

    public void Subscriber()
    {
        Dispatcher.Subscribe(EventId.MapSelectionAppeared, ActivateUpdateComponents);
        Dispatcher.Subscribe(EventId.OnLanguageChange, ActivateUpdateComponents);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.MapSelectionAppeared, ActivateUpdateComponents);
        Dispatcher.Unsubscribe(EventId.OnLanguageChange, ActivateUpdateComponents);
    }

    void ActivateUpdateComponents(EventId _id, EventInfo _info)
    {
        UpdateComponents();
    }

    private void UpdateComponents()
    {   
        lblCount.text = Count.ToString();
        //  lblDescription.text = string.Format("id = {0}, sprite = {1}", ConsumableId, GameData.consumableInfos[ConsumableId].icon);
        lblDescription.text = GameData.consumableInfos[ConsumableId].LocalizedDescription;
        setPriceScript.Price = GameData.consumableInfos[ConsumableId].price.ToPrice();
        HelpTools.SetSpriteToAllSpritesInCollection(sprites, GameData.consumableInfos[ConsumableId].icon);

        if (lblLotSize)
            lblLotSize.text = string.Format("{0} {1}", GameData.consumableInfos[ConsumableId].countToBuy, Localizer.GetText("pieces"));

        if (ConsumableInventory.Instance != null && ConsumableInventory.battleConsumablesSlotsIds.ContainsKey(ConsumableId))
        {
            // обновляем также кол-во боевой расходки , если какой-то слот заполнен этой расходкой
            ConsumableInventory.InventoryItems[ConsumableInventory.battleConsumablesSlotsIds[ConsumableId]].UpdateSlot();
        }
    }

    public override Vector2 Size
    {
        get { return new Vector2(sizeBg.dimensions.x * sizeBg.scale.x, sizeBg.dimensions.y * sizeBg.scale.y); }
    }
}
