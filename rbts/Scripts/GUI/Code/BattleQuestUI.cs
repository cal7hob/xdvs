using UnityEngine;

public class BattleQuestUI : InterfaceModuleBase
{
    [SerializeField] private tk2dTextMesh questProgress;
    [SerializeField] private GameObject sprComplete;
    [SerializeField] private bool showQuestProgressOnNextString = false;

    private bool isItitiated = false;
    private bool needToShow = false;
    private Http.BattleServer bs;

    protected override void Awake()
    {
        base.Awake();
        if(ProfileInfo.IsBattleTutorialCompleted)
        {
            Messenger.Subscribe(EventId.MainTankAppeared, Init);
            Messenger.Subscribe(EventId.StatTableVisibilityChange, OnStatTableChangeVisibility);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Messenger.Unsubscribe(EventId.MainTankAppeared, Init);
        Messenger.Unsubscribe(EventId.OnLanguageChange, OnQuestUpdated);
        Messenger.Unsubscribe(EventId.BattleQuestUpdated, OnQuestUpdated);
        Messenger.Unsubscribe(EventId.StatTableVisibilityChange, OnStatTableChangeVisibility);
    }

    private void Init(EventId evId, EventInfo ev)
    {
        bs = Http.Manager.BattleServer;
        if (bs.quest == null)
        {
            needToShow = false;
            SetActive(false);
            return;
        }

        if (!isItitiated)
        {
            Messenger.Subscribe(EventId.OnLanguageChange, OnQuestUpdated);
            Messenger.Subscribe(EventId.BattleQuestUpdated, OnQuestUpdated);
        }

        SetActive(true);
        needToShow = true;
        OnQuestUpdated(EventId.BattleQuestUpdated, null);
        isItitiated = true;
    }


    private void OnQuestUpdated (EventId evId, EventInfo ev)
    {
        //Модифицируем текст квеста и проверяем квест на выполнение.
        switch (bs.quest.type)
        {
            case Quest.Type.Revenge:
                questProgress.text = bs.quest.LocalizedDescription;
                break;
            default:
                questProgress.text
                    = string.Format(
                        "{0}{1}({2} / {3})",
                        bs.quest.LocalizedDescription,
                        showQuestProgressOnNextString ? "\n" : " ",
                        bs.quest.progress,
                        bs.quest.CompleteCount);
                break;
        }
        sprComplete.SetActive (bs.quest.isComplete);
    }

    private void Update()
    {
        if (!isItitiated || BattleController.MyVehicle == null || bs == null || bs.quest == null) {
            return;
        }
        if (bs.quest.isComplete)
            return;

        if (bs.quest.type == Quest.Type.Mileage) {
            bs.quest.progress = ((int)BattleController.MyVehicle.Odometer);
            if (bs.quest.progress >= bs.quest.CompleteCount) {
                bs.quest.isComplete = true;
            }
            OnQuestUpdated(EventId.BattleQuestUpdated, null);

            // Показываем галочку, если квест выполнен.
            if (bs.quest.isComplete)
            {
                sprComplete.SetActive(true);
                Messenger.Send(EventId.QuestCompleted, new EventInfo_I((int)bs.quest.type));
            }
        }

    }

    private void OnStatTableChangeVisibility(EventId id, EventInfo info)
    {
        SetActive(needToShow && !((EventInfo_B)info).bool1);
    }
}
