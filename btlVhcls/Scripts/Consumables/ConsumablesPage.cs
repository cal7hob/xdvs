using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using XDevs;

public class ConsumablesPage : HangarPage
{
    //!!!!! Порядок ссылок в tk2dUIToggleButtonGroup должен быть такой же как в этом енаме !!!!!
    public enum Tab
    {
        Consumables = 0,
        Kits = 1,
        SuperWeapons = 2,
    }

    public static ConsumablesPage Instance { get; private set; }

    [SerializeField] private tk2dUIToggleButtonGroup tabs;
    [SerializeField] private GameObject[] pages;//порядок как в енаме Tab
    [SerializeField] private Factory consumablesFactory;
    [SerializeField] private Factory kitsFactory;
    [SerializeField] private Factory superWeaponsFactory;
    [SerializeField] private SaleSticker consumableKitsTabSaleSticker;
    public InventoryBase inventoryPanel;
    public InventoryBase superWeaponsInventoryPanel;

    private bool isWaitingServerAnswer = false;

    protected override void Create()
    {
        base.Create();
        Instance = this;
        Dispatcher.Subscribe(EventId.DiscountStateChanged, OnDiscountStateChanged);
    }

    protected override void Destroy()
    {
        base.Destroy();
        Instance = null;
        Dispatcher.Unsubscribe(EventId.DiscountStateChanged, OnDiscountStateChanged);
    }

    /// <summary>
    /// AfterHangarInit
    /// </summary>
    protected override void Init()
    {
        base.Init();
        //Не инстанируем расходку если еще не были в туторе
        if (!ProfileInfo.IsBattleTutorialCompleted)
            return;

        #region Инстанирование объектов
        if (GameData.consumableInfos != null)
        {
            //Инстанируем расходку
            List<int> list = GetConsumablesData();
            #region Test Data
            //list.AddRange(list);
            #endregion
            consumablesFactory.CreateAll(list, true, new ParamDict().Add("inventoryPanel", inventoryPanel ));

            //Инстанируем супероружие
            if (superWeaponsFactory)
                superWeaponsFactory.CreateAll(GetSuperWeaponsData(), true, new ParamDict().Add("inventoryPanel", superWeaponsInventoryPanel) );

            //Инстанируем итемы инвентаря расходки и супероружия
            inventoryPanel.CreateEmptyItems();
            if (superWeaponsInventoryPanel)
                superWeaponsInventoryPanel.CreateEmptyItems();
        }

        if(GameData.consumableKitInfos != null && kitsFactory)
        {
            //Инстанируем наборы расходки
            kitsFactory.CreateAll(GetConsumableKitsData());
        }
        #endregion
    }

    public List<int> GetConsumablesData()
    {
        return GameData.consumableInfos.Values.Where(cons => !cons.isSuperWeapon && (!cons.isHidden || ProfileInfo.HaveConsumable(cons.id))).OrderBy(p => p.position).Select(cons => (int)cons.id).ToList();
    }

    public List<int> GetSuperWeaponsData()
    {
        return GameData.consumableInfos.Values.Where(cons => cons.isSuperWeapon && (!cons.isHidden || ProfileInfo.HaveConsumable(cons.id))).OrderBy(p => p.position).Select(cons => (int)cons.id).ToList();
    }

    public List<int> GetConsumableKitsData()
    {
        return GameData.consumableKitInfos.Values.Where(kit => !kit.isHidden).OrderBy(p => p.position).Select(kit => (int)kit.id).ToList();
    }

    private void OnClick(tk2dUIItem btn)
    {
        switch(btn.name)
        {
            case "btnToBattle":
                SaveInventoryPanelsContentToProfile();
                //ProfileInfo.SaveToServer();//Сохранение инвентаря расходки. Пока убрал, все равно профиль сохранится перед уходом в бой.
                HangarController.Instance.EnterBattle((GameManager.MapId)ProfileInfo.lastMapId);
                break;
        }
    }

    protected override void Show()
    {
        base.Show();
        //Debug.LogWarning("!!!!!! ConsumablesPage.Show()");
        inventoryPanel.FillInventory();
        if(superWeaponsInventoryPanel)
            superWeaponsInventoryPanel.FillInventory();
        UpdateElements();
    }

    protected override void Hide()
    {
        base.Hide();
        SaveInventoryPanelsContentToProfile();
    }

    private void SaveInventoryPanelsContentToProfile()
    {
        ProfileInfo.consumableInventoryPanelItems = ConsumablesInventoryPanel.inventoryList;
        ProfileInfo.superWeaponsInventoryPanelItems = SuperWeaponsInventoryPanel.inventoryList;
    }

    public void OnTabChanged(tk2dUIToggleButtonGroup buttonGroup)
    {
        //Debug.LogError("buttonGroup = " + buttonGroup.gameObject.name);
        for (int i = 0; i < pages.Length; i++)
            pages[i].SetActive(tabs.SelectedIndex == i);
    }

    public void BuyConsumable(int id, Action beforeRequest, Action afterResult)
    {
        if (isWaitingServerAnswer)
            return;

        if (ProfileInfo.CanBuy(GameData.consumableInfos[id].price.ToPrice()))
        {
            isWaitingServerAnswer = true;
            var request = Http.Manager.Instance().CreateRequest("/shop/buyConsumable");
            request.Form.AddField("consumableId", id);

            beforeRequest();

            Http.Manager.StartAsyncRequest(
                request: request,
                successCallback: delegate (Http.Response result)
                {
                    if (GameData.IsHangarScene)//На случай если ответ получим уже в бою
                    {
                        isWaitingServerAnswer = false;
                        afterResult();
                        HangarController.Instance.PlaySound(HangarController.Instance.buyingSound);
                    }

                    Dispatcher.Send(EventId.ConsumableBought, new EventInfo_I(id));
                },
                failCallback: delegate (Http.Response result)
                {
                    if (GameData.IsHangarScene)//На случай если ответ получим уже в бою
                    {
                        isWaitingServerAnswer = false;
                        afterResult();
                    }
                });
        }
        else
            HangarController.Instance.GoToBank(GameData.consumableInfos[id].price.ToPrice().currency);
    }

    public void SetTab(Tab _tab)
    {
        if(((int)_tab) != tabs.SelectedIndex)
            tabs.SelectedIndex = (int)_tab;
    }

    private void OnDiscountStateChanged(EventId id, EventInfo info)
    {
        EventInfo_U evInfo = (EventInfo_U)info;
        EntityTypes entityType = (EntityTypes)evInfo[0];
        int entityId = (int)evInfo[1];
        //bool discountState = (bool)evInfo[2];

        if (entityType == EntityTypes.consumableKit)
            UpdateElements();
    }

    public void UpdateElements()
    {
        consumableKitsTabSaleSticker.sprSaleSticker.SetActive(GameData.HaveConsumableKitsAnyDiscount);
        if (consumableKitsTabSaleSticker.sprSaleSticker.activeSelf)
            consumableKitsTabSaleSticker.SetTextWithFormatString(GameData.MaxConsumableKitsDiscount);
    }
}
