using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XDevs;

public class ConsumablesPage : HangarPage
{
    //!!!!! Порядок ссылок в tk2dUIToggleButtonGroup должен быть такой же как в этом енаме !!!!!
    public enum Tab
    {
        Consumables = 0,
        Kits = 1,
    }

    public static ConsumablesPage Instance { get; private set; }

    [SerializeField] private tk2dUIToggleButtonGroup tabs;
    [SerializeField] private GameObject[] pages;//порядок как в енаме Tab
    [SerializeField] private Factory consumablesFactory;
    [SerializeField] private Factory kitsFactory;
    [SerializeField] private SaleSticker consumableKitsTabSaleSticker;
    [SerializeField] private tk2dUIItem btnToBattleUIItem;
    public ConsumablesInventory inventoryPanel;

    private bool isWaitingServerAnswer = false;
    private List<ConsumableInfo> consumablesList = new List<ConsumableInfo>();

    protected override void Create()
    {
        base.Create();

        if (Instance != null)
        {
            Debug.LogErrorFormat("{0}: Instance already exists!", typeof(ConsumablesPage).ToString());
        }

        Instance = this;
        Messenger.Subscribe(EventId.ChangeConsumableInventoryState, ChangeConsumableInventoryState);
        Messenger.Subscribe(EventId.DiscountStateChanged, OnDiscountStateChanged);
        btnToBattleUIItem.OnClick += OnBtnToBattleClickHandler;
    }

    protected override void Destroy()
    {
        base.Destroy();
        Messenger.Unsubscribe(EventId.ChangeConsumableInventoryState, ChangeConsumableInventoryState);
        Messenger.Unsubscribe(EventId.DiscountStateChanged, OnDiscountStateChanged);
        btnToBattleUIItem.OnClick -= OnBtnToBattleClickHandler;
        Instance = null;
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
            consumablesList = GameData.consumableInfos.Values.Where(cons => !cons.isHidden || ProfileInfo.HaveConsumable(cons.id)).OrderBy(p => p.position).ToList();

            foreach (var consumableInfo in consumablesList)
            {
                var consumablePanelItem = consumablesFactory.Create((int)consumableInfo.id);

                if (consumablePanelItem == null)
                    continue;

                var consItem = consumablePanelItem as HangarConsumableItem;

                if (consItem == null)
                    continue;

                consItem.UpdateComponents();
            }
        }

        #endregion

        #region Наборы

        if (GameData.consumableKitInfos != null && kitsFactory)
        {
            //Инстанируем наборы расходки
            // VEHICLES kitsFactory.CreateAll(GetConsumableKitsData());

            // Нужна работа с табами. Если есть доступные киты — включаем таб, если нет — выключаем.

            if (GetConsumableKitsData().Count > 0)
            {
                tabs.ToggleBtns[(int)Tab.Kits].gameObject.SetActive(true);
            }

            //foreach (var consumableInfo in consumablesList)
            //{
            //    var consumablePanelItem = consumablesFactory.Create((int) consumableInfo.id);
            //}


            foreach (var kitId in GetConsumableKitsData())
                kitsFactory.Create(kitId);
        }

        #endregion

        #region Тестовые данные для наборов расходок
        //List<int> list = new List<int>() { 1,2,3,4,5};
        //foreach (var kitId in list)
        //    kitsFactory.Create(kitId);
        #endregion

        inventoryPanel.Reset();
    }

    public List<int> GetConsumableKitsData()
    {
        return GameData.consumableKitInfos.Values.Where(kit => !kit.isHidden).OrderBy(p => p.position).Select(kit => (int)kit.id).ToList();
    }

    private void OnBtnToBattleClickHandler()
    {
        //Сохраняем в профиль текущий инвентарь
        ProfileInfo.consumableInventoryPanelItems = ConsumablesInventory.battleInventoryList;
        //ProfileInfo.SaveToServer();//Сохранение инвентаря расходки. Пока убрал, все равно профиль сохранится перед уходом в бой.
        HangarController.Instance.EnterBattle((GameManager.MapId)ProfileInfo.lastMapId);
    }

    protected override void Show()
    {
        base.Show();
        if (consumablesFactory.Items.Count > 0)
            FillInventoryPanel();

        UpdateElements();
    }

    protected override void Hide()
    {
        base.Hide();
        ProfileInfo.consumableInventoryPanelItems = ConsumablesInventory.battleInventoryList;
    }

    public void OnTabChanged(tk2dUIToggleButtonGroup buttonGroup)
    {
        //Debug.LogError("buttonGroup = " + buttonGroup.gameObject.name);
        for (int i = 0; i < pages.Length; i++)
            pages[i].SetActive(tabs.SelectedIndex == i);
    }

    /// <summary>
    /// Автозаполнение панели инвентаря
    /// </summary>
    private void FillInventoryPanel()
    {
        #region Сначала пытаемся поставить в панель итемы из профиля
        for (int i = 0; i < ConsumablesInventory.CAPACITY; i++)
        {
            //если в профиле сохранен текущий слот                          и у меня есть расходка из этого слота - добавляем в инвентарь
            if (i < ProfileInfo.consumableInventoryPanelItems.Count && ProfileInfo.HaveConsumable(ProfileInfo.consumableInventoryPanelItems[i]))
                SetConsumableToInventory(ProfileInfo.consumableInventoryPanelItems[i], true, i);
        }
        #endregion

        if (!inventoryPanel.HasEmptyCell())
            return;

        #region затем заполняем оставшиеся пустые ячейки
        List<ConsumableInfo> unaddedConsumables = new List<ConsumableInfo>();//недобавленные в инвентарь расходки
        for (int i = 0; i < consumablesList.Count; i++)
        {
            ConsumablesInventoryItem inventoryItem = inventoryPanel.GetInventoryCellByConsumableId(consumablesList[i].id);
            if (!inventoryItem && ProfileInfo.HaveConsumable(consumablesList[i].id))
                unaddedConsumables.Add(consumablesList[i]);
        }

        int cycleLength = Math.Min(inventoryPanel.EmptyCellsCount, unaddedConsumables.Count);//определяем сколько расходок надо добавлять в инвентарь
        for (int i = 0; i < cycleLength; i++)
            SetConsumableToInventory(unaddedConsumables[i].id, true);
        #endregion
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

                        ConsumablesInventoryItem inventoryItem = inventoryPanel.GetInventoryCellByConsumableId(id);

                        if (inventoryItem)
                            inventoryItem.UpdateElements();
                        else
                            SetConsumableToInventory(id, true);

                        HangarController.Instance.PlayGUISound(HangarController.Instance.buyingSound);
                    }

                    Messenger.Send(EventId.ConsumableBought, new EventInfo_I(id));
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

    private void ChangeConsumableInventoryState(EventId id, EventInfo ei)
    {
        EventInfo_IB info = (EventInfo_IB)ei;

        int consId = info.int1;
        bool status = info.bool1;

        //Debug.LogErrorFormat("ChangeConsumableInventoryState. {0}", consId);

        SetConsumableToInventory(consId, status, -1);
    }

    /// <summary>
    /// Установка расходки в панель инвентаря. При указании номера слота - в
    /// </summary>
    /// <param name="consId">Ид расходки</param>
    /// <param name="status">true - взять в бой, false - не брать в бой</param>
    /// <param name="slot">желаемый номер слота</param>
    private void SetConsumableToInventory(int consId, bool status, int slot = -1)
    {
        if (status && ProfileInfo.HaveConsumable(consId) && !inventoryPanel.HasConsumable(consId))
        {
            if (inventoryPanel.HasEmptyCell() && !inventoryPanel.Add(consId, slot))
                Debug.LogWarningFormat("Cant add consumable {0} to slot {1}!", consId, slot);
        }
        else
        {
            if (!status)
                inventoryPanel.Remove(consId);

            if (!ProfileInfo.HaveConsumable(consId))
                return;
        }

        //Если итем есть в инвентаре - отмечаем на нем галочку, иначе снимаем ее.
        IItem iface = consumablesFactory.GetItemByUniqId(consId.ToString());

        if (iface != null)
        {
            var consItem = iface as HangarConsumableItem;

            if (consItem == null)
                return;

            consItem.IsForBattle = inventoryPanel.HasConsumable(consId);

            consItem.UpdateComponents();
        }
        else
            Debug.LogErrorFormat("ChangeConsumableInventoryState. Cant find item by Id {0}", consId);
    }

    private void OnDiscountStateChanged(EventId id, EventInfo info)
    {
        EventInfo_U evInfo = (EventInfo_U)info;
        EntityTypes entityType = (EntityTypes)evInfo[0];
        int entityId = (int)evInfo[1];

        if (entityType == EntityTypes.ConsumableKit)
            UpdateElements();
    }

    public void UpdateElements()
    {
        consumableKitsTabSaleSticker.sprSaleSticker.SetActive(GameData.HaveConsumableKitsAnyDiscount);
        if (consumableKitsTabSaleSticker.sprSaleSticker.activeSelf)
            consumableKitsTabSaleSticker.SetTextWithFormatString(GameData.MaxConsumableKitsDiscount);
    }
}
