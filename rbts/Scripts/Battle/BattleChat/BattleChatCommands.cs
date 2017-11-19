using UnityEngine;
using System.Collections.Generic;

public class BattleChatCommands : InterfaceModuleBase
{
    /// <summary>
    ////Индексы не менять (приведет к несовместимости версий)
    /// </summary>
    public enum Id
    {
        Attack              = 0,// В атаку!
        Affirmative         = 1,// Так точно!
        HelpMe              = 2,// Помоги!
        NotInterfere        = 3,// Не мешай!
        Negative            = 4,// Никак нет!
        Thanks              = 5,// Спасибо!
        Killing             = 6,// Убийство
    }

    public static BattleChatCommands Instance { get; private set; }

    private float lastCommandSendTime = 0;
    private bool CanSend { get { return Time.realtimeSinceStartup > lastCommandSendTime + GameData.battleChatMessageSendInterval; } }

    protected override void Awake()
    {
        Instance = this;
        base.Awake();
        Messenger.Subscribe(EventId.StatTableVisibilityChange, OnStatTableChangeVisibility);
        Messenger.Subscribe(EventId.MessageBoxChangeVisibility, OnMessageBoxChangeVisibility);
        Messenger.Subscribe(EventId.NotifierChangeVisibility, OnNotifierChangeVisibility);
        Messenger.Subscribe(EventId.BattleSettingsChangeVisibility, OnBattleSettingsChangeVisibility);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Messenger.Unsubscribe(EventId.StatTableVisibilityChange, OnStatTableChangeVisibility);
        Messenger.Unsubscribe(EventId.MessageBoxChangeVisibility, OnMessageBoxChangeVisibility);
        Messenger.Unsubscribe(EventId.NotifierChangeVisibility, OnNotifierChangeVisibility);
        Messenger.Unsubscribe(EventId.BattleSettingsChangeVisibility, OnBattleSettingsChangeVisibility);
        Instance = null;
    }

    private void OnCommandClick(tk2dUIItem btn )
    {
        BattleChatCommandItem item = btn.GetComponent<BattleChatCommandItem>();
        //Debug.Log("item = " + item.id);
        SendChatMessage(BattleController.MyVehicle.data.playerId, item.id);
        Hide();
    }

    private void OpenChatCommands(tk2dUIItem btn)
    {
        #region Testing HightPingAlarm
        //Dispatcher.Send(EventId.HighPingAlarm, new EventInfo_B(false));
        //return;
        #endregion

        if (CanSend)
            Toggle();
    }

    private void OnStatTableChangeVisibility(EventId id, EventInfo info)
    {
        if (((EventInfo_B)info).bool1 && IsActive)
            SetActive(false);
    }

    private void OnMessageBoxChangeVisibility(EventId id, EventInfo info)
    {
        if (((EventInfo_B)info).bool1 && IsActive)
            SetActive(false);
    }

    private void OnNotifierChangeVisibility(EventId id, EventInfo info)
    {
        if (((EventInfo_B)info).bool1 && IsActive)
            SetActive(false);
    }

    private void OnBattleSettingsChangeVisibility(EventId id, EventInfo info)
    {
        if (((EventInfo_B)info).bool1 && IsActive)
            SetActive(false);
    }

    public override void AfterStateChange()
    {
        Messenger.Send(EventId.BattleChatCommandsChangeVisibility, new EventInfo_B(IsActive));
    }

    private void SendChatMessage(int photonPlayerId, Id messageId)
    {
        List<int> receivers = BattleController.GetVehiclesIdList(BattleController.VehiclesSet.AllInDeathMatchOrYourTeamExcludeBots);
        EventInfo_U eventData = new EventInfo_U(photonPlayerId, (int)messageId);
        lastCommandSendTime = Time.realtimeSinceStartup;
        for (int i = 0; i < receivers.Count; i++)
            Messenger.Send(EventId.BattleChatCommand, eventData, Messenger.EventTargetType.ToSpecific, receivers[i]);
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
}
