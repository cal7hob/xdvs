using UnityEngine;
using System;

public class Module_ButtonOpenBattleCommands : InterfaceModuleBase
{
    [SerializeField] private ActivatedUpDownButton activationScript;
	[SerializeField] private ProgressBar delayProgressBar;
    private float counter = 0;

    protected override void Awake()
    {
        if (!ProfileInfo.IsBattleTutorialCompleted)
        {
            SetActive(false);
            return;
        }
        base.Awake();
        Messenger.Subscribe(EventId.StatTableVisibilityChange, OnStatTableChangeVisibility);
        Messenger.Subscribe(EventId.BattleChatCommand, OnBattleChatCommand);
		if (delayProgressBar)
			delayProgressBar.Percentage = 0;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Messenger.Unsubscribe(EventId.StatTableVisibilityChange, OnStatTableChangeVisibility);
        Messenger.Unsubscribe(EventId.BattleChatCommand, OnBattleChatCommand);
    }

    private void OnStatTableChangeVisibility(EventId id, EventInfo info)
    {
        SetActive(!((EventInfo_B)info).bool1);
    }

    private void OnBattleChatCommand(EventId id, EventInfo info)
    {
        EventInfo_U eventData = (EventInfo_U)info;
        if (Convert.ToInt32(eventData[0]) == BattleController.MyPlayerId)//Если я послал сообщение в чат - дизейблим кнопку отправки на время N
            activationScript.Activated = false;
    }

    private void Update()
    {
		if (activationScript.Activated) //Анимация недоступности
			return;
		counter += Time.deltaTime;
		if (delayProgressBar)
			delayProgressBar.Percentage = counter / (float)GameData.battleChatMessageSendInterval;
		if (counter >= GameData.battleChatMessageSendInterval) 
		{
			activationScript.Activated = true;
			counter = 0;
			if (delayProgressBar)
				delayProgressBar.Percentage = 0;
		}

    }

}
