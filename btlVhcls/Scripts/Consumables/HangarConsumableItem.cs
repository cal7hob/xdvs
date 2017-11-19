using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class HangarConsumableItem : MonoBehaviour, IItem
{
    public tk2dSlicedSprite sizeBg;//для определения размера итема

    [SerializeField] protected tk2dTextMesh lblDescription;
    [SerializeField] private tk2dTextMesh lblCount;
    [SerializeField] protected tk2dUIToggleControl checkBox_ForBattle;
    [SerializeField] protected tk2dBaseSprite[] sprites;//специфичная для итема текстура
    [SerializeField] protected PriceRenderer setPriceScript;
    [SerializeField] protected ActivatedUpDownButton btnBuy;
    [SerializeField] private tk2dTextMesh lblLotSize;//Количество единиц расходки в паке
    [SerializeField] protected GameObject vipIconWrapper;
    [SerializeField] protected tk2dUIItem uiitem;

    public int ConsumableId { get; private set; }
    public int Count { get { return (ProfileInfo.HaveConsumable(ConsumableId)) ? (int)ProfileInfo.consumableInventory[ConsumableId].count : 0; } }
    public bool IsAlive { get { return Count > 0 && (ProfileInfo.consumableInventory[ConsumableId].deathTime - GameData.CurrentTimeStamp) > 0; } }

    protected InventoryBase targetInventoryPanel = null;

    protected virtual void Awake()
    {
        Dispatcher.Subscribe(EventId.OnLanguageChange, OnLanguageChange);
        Dispatcher.Subscribe(EventId.ConsumableInventoryStateChanged, OnConsumableInventoryStateChanged);
        Dispatcher.Subscribe(EventId.ProfileInfoLoadedFromServer, OnProfileInfoLoadedFromServer);

        UpdateElements();
    }

    protected virtual void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.OnLanguageChange, OnLanguageChange);
        Dispatcher.Unsubscribe(EventId.ConsumableInventoryStateChanged, OnConsumableInventoryStateChanged);
        Dispatcher.Unsubscribe(EventId.ProfileInfoLoadedFromServer, OnProfileInfoLoadedFromServer);
    }

    private void OnClick(tk2dUIItem btn)
    {
        switch (btn.name)
        {
            case "btnBuy":
                if (!GameData.consumableInfos[ConsumableId].IsEnabledByVipStatus)
                {
                    Dispatcher.Send(EventId.VipConsumableClicked, new EventInfo_I(ConsumableId));

                    HangarController.Instance.NavigateToVipShop(
                        showMessageBox:     true,
                        negativeCallback:   () =>
                                            {
                                            },
                        positiveCallback:   () =>
                                            {
                                                GoogleAnalyticsDispatcher.StartEventChain(GAEvent.Category.VIPAccountBoughtAfterVIPConsumableClicked, ConsumableId);
                                            });
                    return;
                }
                ConsumablesPage.Instance.BuyConsumable(ConsumableId, () => { btnBuy.Activated = false; }, () => { btnBuy.Activated = true; });
                break;
        }
    }

    private void OnItemClick(tk2dUIItem btn)
    {
        Dispatcher.Send(EventId.ChangeConsumableInventoryState, new EventInfo_U(ConsumableId, !checkBox_ForBattle.IsOn, -1));
    }

    public void Initialize(object[] parameters)
    {
        ConsumableId = (int)parameters[0];
        ParamDict additionalParams = (ParamDict)parameters[1];
        targetInventoryPanel = (InventoryBase)additionalParams.GetValue("inventoryPanel");

        UpdateElements();
    }

    public virtual void UpdateElements()
    {
        //Debug.LogErrorFormat("{0}. ConsumableId {1}", targetInventoryPanel.GetType(), ConsumableId);
        lblCount.text = Count.ToString();
        //lblDescription.text = string.Format("id = {0}, sprite = {1}", ConsumableId, GameData.consumableInfos[ConsumableId].icon);
        lblDescription.text = GameData.consumableInfos[ConsumableId].LocalizedDescription;
        setPriceScript.Price = GameData.consumableInfos[ConsumableId].price.ToPrice();
        HelpTools.SetSpriteToAllSpritesInCollection(sprites, GameData.consumableInfos[ConsumableId].GetIcon(withFrame: true));
        vipIconWrapper.SetActive(GameData.consumableInfos[ConsumableId].isVip);
        if (lblLotSize)
            lblLotSize.text = string.Format("{0} {1}", GameData.consumableInfos[ConsumableId].countToBuy, Localizer.GetText("pieces"));
        if (GameData.consumableInfos[ConsumableId].isHidden)
            btnBuy.Activated = false;
        //пока targetInventoryPanel не проснулась - не имеет смысла. Если еще не проснулась, значит будет автозаполнение в InventoryBase.Start()
        checkBox_ForBattle.IsOn = targetInventoryPanel.IsAwaked ? targetInventoryPanel.HasFactoryItemWithContentId(ConsumableId) : false;
    }

    public void DesrtoySelf()
    {

    }

    public Vector2 GetSize()
    {
        return new Vector2(sizeBg.dimensions.x * sizeBg.scale.x, sizeBg.dimensions.y * sizeBg.scale.y);
    }

    private void OnLanguageChange(EventId evId, EventInfo ev)
    {
        UpdateElements();
    }

    private void OnConsumableInventoryStateChanged(EventId evId, EventInfo ev)
    {
        EventInfo_U eventInfo = (EventInfo_U)ev;
        int consId = (int)eventInfo[0];
        bool state = (bool)eventInfo[1];
        if (consId != ConsumableId)
            return;

        UpdateElements();
    }

    private void OnProfileInfoLoadedFromServer(EventId evId, EventInfo ev)
    {
        UpdateElements();
    }

    public string GetUniqId { get { return ConsumableId.ToString(); } }

    public tk2dUIItem MainUIItem { get { return uiitem; } }

    public Transform MainTransform { get { return transform; } }
}
