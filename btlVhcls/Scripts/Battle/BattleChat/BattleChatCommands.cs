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
        Dispatcher.Subscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);
        Dispatcher.Subscribe(EventId.MessageBoxChangeVisibility, OnMessageBoxChangeVisibility);
        Dispatcher.Subscribe(EventId.OnNotifierChangeVisibility, OnNotifierChangeVisibility);
        Dispatcher.Subscribe(EventId.OnBattleSettingsChangeVisibility, OnBattleSettingsChangeVisibility);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Dispatcher.Unsubscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);
        Dispatcher.Unsubscribe(EventId.MessageBoxChangeVisibility, OnMessageBoxChangeVisibility);
        Dispatcher.Unsubscribe(EventId.OnNotifierChangeVisibility, OnNotifierChangeVisibility);
        Dispatcher.Unsubscribe(EventId.OnBattleSettingsChangeVisibility, OnBattleSettingsChangeVisibility);
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
        Dispatcher.Send(EventId.OnBattleChatCommandsChangeVisibility, new EventInfo_B(IsActive));
    }

    private void SendChatMessage(int photonPlayerId, Id messageId)
    {
        List<int> receivers = BattleController.GetVehiclesIdList(BattleController.VehiclesSet.AllInDeathMatchOrYourTeamExcludeBots);
        EventInfo_U eventData = new EventInfo_U(photonPlayerId, (int)messageId);
        lastCommandSendTime = Time.realtimeSinceStartup;
        for (int i = 0; i < receivers.Count; i++)
            Dispatcher.Send(EventId.BattleChatCommand, eventData, Dispatcher.EventTargetType.ToSpecific, receivers[i]);
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
