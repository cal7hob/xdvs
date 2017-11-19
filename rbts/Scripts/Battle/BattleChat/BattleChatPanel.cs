using UnityEngine;
using System;
using System.Collections.Generic;

public class BattleChatPanel : InterfaceModuleBase
{
    public static BattleChatPanel Instance { get; private set; }
    [SerializeField] private Transform itemsParent;
    [SerializeField] private BattleChatPanelItem prefab;
    [Header("true - новый итем сверху")]
    [SerializeField] private bool itemsDirection = true;
    [SerializeField] private float itemHeight = 60;
    [SerializeField] private float distanceBetweenItems = 10;

    private Dictionary<int, BattleChatPanelItem> itemsDic = new Dictionary<int, BattleChatPanelItem>();
    private List<BattleChatPanelItemData> itemsData = new List<BattleChatPanelItemData>();

    protected override void Awake()
    {
        Instance = this;
        base.Awake();
        Messenger.Subscribe(EventId.StatTableVisibilityChange, OnStatTableChangeVisibility);
        Messenger.Subscribe(EventId.MessageBoxChangeVisibility, OnMessageBoxChangeVisibility);
        Messenger.Subscribe(EventId.BattleChatCommandsChangeVisibility, OnBattleChatCommandsChangeVisibility);
        Messenger.Subscribe(EventId.BattleChatCommand, OnBattleChatCommand);
        Messenger.Subscribe(EventId.BattleSettingsChangeVisibility, OnBattleSettingsChangeVisibility);
        Messenger.Subscribe(EventId.BattleEnd, OnBattleEnd);
        Messenger.Subscribe(EventId.VehicleKilled, OnTankKilled);

        InstantiateItems();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Messenger.Unsubscribe(EventId.StatTableVisibilityChange, OnStatTableChangeVisibility);
        Messenger.Unsubscribe(EventId.MessageBoxChangeVisibility, OnMessageBoxChangeVisibility);
        Messenger.Unsubscribe(EventId.BattleChatCommandsChangeVisibility, OnBattleChatCommandsChangeVisibility);
        Messenger.Unsubscribe(EventId.BattleChatCommand, OnBattleChatCommand);
        Messenger.Unsubscribe(EventId.BattleSettingsChangeVisibility, OnBattleSettingsChangeVisibility);
        Messenger.Unsubscribe(EventId.BattleEnd, OnBattleEnd);
        Messenger.Unsubscribe(EventId.VehicleKilled, OnTankKilled);
        Instance = null;
    }

    private void OnStatTableChangeVisibility(EventId id, EventInfo info)
    {
        UpdateVisibility();
    }

    private void OnMessageBoxChangeVisibility(EventId id, EventInfo info)
    {
        UpdateVisibility();
    }

    private void OnBattleChatCommandsChangeVisibility(EventId id, EventInfo info)
    {
        UpdateVisibility();
    }

    private void OnBattleSettingsChangeVisibility(EventId id, EventInfo info)
    {
        UpdateVisibility();
    }

    private void OnBattleEnd(EventId id, EventInfo info)
    {
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        bool mustHide = BattleController.IsBattleFinished || MessageBox.IsShown || StatTable.OnScreen || BattleSettings.OnScreen || BattleChatCommands.OnScreen;
        SetActive(!mustHide);
    }

    public static bool OnScreen
    {
        get
        {
            if (Instance == null)
                return false;
            return Instance.IsActive;
        }
    }

    private void InstantiateItems()
    {
        itemsDic = new Dictionary<int, BattleChatPanelItem>();
        int sign = itemsDirection ? -1 : 1;
        for (int i = 0; i < GameData.battleChatPanelItemsMaxCount; i++)
        {
            itemsDic.Add(i, Instantiate(prefab));
            itemsDic[i].transform.SetParent(itemsParent);
            itemsDic[i].transform.localPosition = new Vector3(0,
                sign * i * (itemHeight + distanceBetweenItems),
                0);
            itemsDic[i].SetActive(false);
        }
    }

    private void OnBattleChatCommand(EventId id, EventInfo info)
    {
        EventInfo_U eventData = (EventInfo_U)info;
        int _photonPlayerId = Convert.ToInt32(eventData[0]);
        if (!BattleController.allVehicles.ContainsKey(_photonPlayerId))
        {
            Debug.LogErrorFormat("BattleChatPanel. Cant find player {0} in BattleController.allVehicles", _photonPlayerId);
            return;
        }
        BattleChatPanelItemData data = new BattleChatPanelItemData(_photonPlayerId, (BattleChatCommands.Id)Convert.ToInt32(eventData[1]), Time.time);
        //Debug.LogFormat("message = {0}, data.showingTime = {1}", data.messageId, data.showingTime);
        AddItem(data);
    }

    private void OnTankKilled(EventId id, EventInfo info)
    {
        EventInfo_II eventData = (EventInfo_II)info;
        int victimId = eventData.int1;
        int attackerId = eventData.int2;

        if (!BattleController.allVehicles.ContainsKey(victimId) || !BattleController.allVehicles.ContainsKey(attackerId))
        {
            Debug.LogErrorFormat("BattleChatPanel. Not all players ({0} or {1}) are in BattleController.allVehicles", victimId, attackerId);
            return;
        }
        BattleChatPanelItemData data = new BattleInfoPanelKillingItemData(attackerId, BattleChatCommands.Id.Killing, Time.time, victimId);
        //Debug.LogFormat("message = {0}, data.showingTime = {1}", data.messageId, data.showingTime);
        AddItem(data);
    }

    public void AddItem(BattleChatPanelItemData data)
    {
        if (BattleController.Instance == null)
            return;
        itemsData.Insert(0, data);
        while (itemsData.Count > GameData.battleChatPanelItemsMaxCount)
            itemsData.RemoveAt(itemsData.Count - 1);
        UpdateChatItems();
    }

    private void UpdateChatItems()
    {
        for (int i = 0; i < GameData.battleChatPanelItemsMaxCount; i++)
        {
            //Debug.LogFormat("itemsData.Count > i = {0}, itemsData[i] != null = {1}, itemsData[i].IsLive = {2}", itemsData.Count > i, itemsData[i] != null, itemsData[i].IsLive);
            if (itemsData.Count > i && itemsData[i] != null && itemsData[i].IsLive)//Если есть данные по такому индексу
            {
                itemsDic[i].Setup(itemsData[i]);
                itemsDic[i].SetActive(true);
            }
            else
            {
                itemsDic[i].SetActive(false);
            }
                
        }
    }
}
