using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ConsumablesPage : InterfaceModuleBase
{
    //!!!!! Порядок ссылок в tk2dUIToggleButtonGroup должен быть такой же как в этом енаме !!!!!
    public enum Tab
    {
        Consumables = 0,
        Kits = 1,
    }

    public static ConsumablesPage Instance { get; private set; }

    [SerializeField]
    private tk2dUIToggleButtonGroup tabs;
    [SerializeField]
    private GameObject[] pages;//порядок как в енаме Tab
    [SerializeField]
    private SimpleItemsPanel consumableItemsPanel;
    [SerializeField]
    private SimpleItemsPanel consumableKitsPanel;
    public ConsumableInventory inventoryPanel;

    private bool isWaitingServerAnswer = false;
    private bool isAutoSetExecuted = false;

    protected override void Awake()
    {
        base.Awake();
        Instance = this;

        Dispatcher.Subscribe(EventId.AfterHangarInit, AfterHangarInit);
        #region Назначение камеры в анкоры
        tk2dCameraAnchor[] anchors = wrapper.GetComponentsInChildren<tk2dCameraAnchor>();
        if (anchors != null)
            for (int i = 0; i < anchors.Length; i++)
                anchors[i].AnchorCamera = GameData.CurSceneGuiCamera;
        #endregion
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Dispatcher.Unsubscribe(EventId.AfterHangarInit, AfterHangarInit);
        Instance = null;
    }

    private void AfterHangarInit(EventId id, EventInfo info)
    {
        if (!HangarCameraController.Instance.nonCamRotationWindows.Contains(gameObject))
        {
            HangarCameraController.Instance.nonCamRotationWindows.Add(gameObject);
        }
        #region Инстанирование объектов
        if (GameData.consumableInfos != null)
            foreach (var pair in GameData.consumableInfos)
            {
                var consumable = consumableItemsPanel.CreateItem<ConsumableItem>();
                consumable.Subscriber();
                consumable.Initialize(pair.Key);
            }
        #endregion
        

       // List<int> list = new List<int>() { 1, 2, 3, 4, 5 };
        //foreach (var kitId in list)
        //{
        //    var consumableKit = consumableKitsPanel.CreateItem<ConsumableKitItem>();
        //    consumableKit.Initialize(kitId);
        //}

        consumableKitsPanel.ScrollableItemsBehaviour.UpdateContentLength();

        inventoryPanel.Reset();
    }

    private void OnClick(tk2dUIItem btn)
    {
        switch (btn.name)
        {
            case "btnToBattle":
                HangarController.Instance.EnterBattle(MapFramesCreator.GetMapForBattle((GameManager.MapId)ProfileInfo.lastMapId));
                break;
        }
    }

    protected override void OnWrapperStateChanged(StateEventSender sender, bool en)
    {
        #region Автозаполнение панели инвентаря
        if (en && !isAutoSetExecuted)
        {
            isAutoSetExecuted = true;
            int count = 0;
            for (int i = 0; i < consumableItemsPanel.ScrollableItems.Count; i++)
            {
                ConsumableItem itemScript = (ConsumableItem)consumableItemsPanel.ScrollableItems[i];
                if (itemScript.Count > 0)
                {
                    itemScript.UiItem.SimulateClick();
                    count++;
                    if (count >= ConsumableInventory.CAPACITY)
                        break;//Заполнили всю панель
                }
            }
        }
        #endregion
    }

    public void OnTabChanged(tk2dUIToggleButtonGroup buttonGroup)
    {
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
                    isWaitingServerAnswer = false;
                    afterResult();
                    Dispatcher.Send(EventId.ConsumableBought, new EventInfo_I(id));
                },
                failCallback: delegate (Http.Response result)
                {
                    isWaitingServerAnswer = false;
                    afterResult();
                });
        }
        else
            HangarController.Instance.GoToBank(GameData.consumableInfos[id].price.ToPrice().currency);
    }
}
