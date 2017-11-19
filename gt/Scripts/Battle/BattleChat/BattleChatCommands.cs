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
    }

    public static Dictionary<int, VoiceEventKey> chatVoiceDict = new Dictionary<int, VoiceEventKey>
    {
        {0, VoiceEventKey.ChatAttack},//Id.Attack
        {1, VoiceEventKey.ChatAffirmative},//Id.Affirmative
        {2, VoiceEventKey.ChatHelpMe},//Id.HelpMe
        {3, VoiceEventKey.ChatNotInterfere},//Id.NotInterfere
        {4, VoiceEventKey.ChatNegative},//Id.Negative
        {5, VoiceEventKey.ChatThanks}//Id.Thanks
    };

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
        BattleSoundManager.Instance.PlaySound(BattleSoundManager.Instance.checkBoxSound);
        BattleChatCommandItem item = btn.GetComponent<BattleChatCommandItem>();
        //Debug.Log("item = " + item.id);
        SendChatMessage(BattleController.MyVehicle.data.playerId, item.id);
        Hide();
    }

    public void OpenChatCommands(tk2dUIItem btn)
    {
        BattleSoundManager.Instance.PlaySound(BattleSoundManager.Instance.buttonClickSound);
        if (CanSend && ProfileInfo.IsBattleTutorialCompleted)
        {
            Toggle();
        }
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
        List<int> receivers = GetReceiversList();
        EventInfo_U eventData = new EventInfo_U(photonPlayerId, (int)messageId);
        lastCommandSendTime = Time.realtimeSinceStartup;
        for (int i = 0; i < receivers.Count; i++)
        {
            Dispatcher.Send(EventId.BattleChatCommand, eventData, Dispatcher.EventTargetType.ToSpecific, receivers[i]);
        }
    }

    private List<int> GetReceiversList()
    {
        List<int> receivers = new List<int>();
        foreach(var vehiclePair in BattleController.allVehicles)
        {
            if (vehiclePair.Value.IsBot || (BattleController.Instance.IsTeamMode && vehiclePair.Value.data.teamId != BattleController.MyVehicle.data.teamId))
                continue;
            receivers.Add(vehiclePair.Key);
        }
        
        return receivers;
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
