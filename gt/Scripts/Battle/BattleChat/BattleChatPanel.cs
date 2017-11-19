using UnityEngine;
using System;
using System.Collections.Generic;

public class BattleChatPanel : InterfaceModuleBase
{
    public static BattleChatPanel Instance { get; private set; }
    [SerializeField]
    private Transform itemsParent;
    [SerializeField]
    private BattleChatPanelItem prefab;
    [Header("true - новый итем сверху")]
    [SerializeField]
    private bool itemsDirection = true;
    [SerializeField]
    private float itemHeight = 60;
    [SerializeField]
    private float distanceBetweenItems = 10;
    [SerializeField]
    private GameObject statTable;

    private Dictionary<int, BattleChatPanelItem> itemsDic = new Dictionary<int, BattleChatPanelItem>();
    private List<BattleChatPanelItemData> itemsData = new List<BattleChatPanelItemData>();
    private bool LastDamageTakenFromLandmine;
    private  EventInfo_II LastDamage = new EventInfo_II();

    protected override void Awake()
    {
        Instance = this;
        base.Awake();


        Dispatcher.Subscribe(EventId.TankKilledInfo, CollectLastDamageInfo);
        Dispatcher.Subscribe(EventId.TankKilled, ShowNecrolog);

        Dispatcher.Subscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);
        Dispatcher.Subscribe(EventId.MessageBoxChangeVisibility, OnMessageBoxChangeVisibility);
        Dispatcher.Subscribe(EventId.OnBattleChatCommandsChangeVisibility, OnBattleChatCommandsChangeVisibility);
        Dispatcher.Subscribe(EventId.BattleChatCommand, OnBattleChatCommand);
        Dispatcher.Subscribe(EventId.OnBattleSettingsChangeVisibility, OnBattleSettingsChangeVisibility);
        InstantiateItems();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        Dispatcher.Unsubscribe(EventId.TankKilledInfo, CollectLastDamageInfo);
        Dispatcher.Unsubscribe(EventId.TankKilled, ShowNecrolog);


        Dispatcher.Unsubscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);
        Dispatcher.Unsubscribe(EventId.MessageBoxChangeVisibility, OnMessageBoxChangeVisibility);
        Dispatcher.Unsubscribe(EventId.OnBattleChatCommandsChangeVisibility, OnBattleChatCommandsChangeVisibility);
        Dispatcher.Unsubscribe(EventId.BattleChatCommand, OnBattleChatCommand);
        Dispatcher.Unsubscribe(EventId.OnBattleSettingsChangeVisibility, OnBattleSettingsChangeVisibility);
        Instance = null;
    }


    void Update()
    {
        if (!statTable.activeInHierarchy) return;
        SetActive(false);
    }

    private void CollectLastDamageInfo(EventId _id, EventInfo _info)
    {
        LastDamage = (EventInfo_II)_info;
    }

    private void ShowNecrolog(EventId _id, EventInfo _info)
    {
        var info = (EventInfo_II)_info;

        // string Killer = BattleController.GameStat[_Info.int2].playerName;
        // string Victim = BattleController.GameStat[_Info.int1].playerName;

        if (LastDamage.int1 == info.int1 && LastDamage.int2 == (int) ShellType.Landmine)
        {
            LastDamageTakenFromLandmine = true;
        }
        BattleChatPanelItemData data = new BattleChatPanelItemData(Convert.ToInt32(info.int2),//Killer
        (BattleChatCommands.Id)Convert.ToInt32(info.int1),//Victim
        Time.realtimeSinceStartup, true, LastDamageTakenFromLandmine//MineKillMeOrNot
        );
        LastDamageTakenFromLandmine = false;
        //Debug.LogFormat("message = {0}, data.showingTime = {1}", data.messageId, data.showingTime);
        AddItem(data);

    }

    private void OnStatTableChangeVisibility(EventId id, EventInfo info)
    {
        SetActive(!((EventInfo_B)info).bool1);
    }

    private void OnMessageBoxChangeVisibility(EventId id, EventInfo info)
    {
        SetActive(!((EventInfo_B)info).bool1);
    }

    private void OnBattleChatCommandsChangeVisibility(EventId id, EventInfo info)
    {
        SetActive(!((EventInfo_B)info).bool1);
    }

    private void OnBattleSettingsChangeVisibility(EventId id, EventInfo info)
    {
        SetActive(!((EventInfo_B)info).bool1);
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
        BattleChatPanelItemData data = new BattleChatPanelItemData(Convert.ToInt32(eventData[0]), (BattleChatCommands.Id)Convert.ToInt32(eventData[1]), Time.realtimeSinceStartup, false, false);
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
