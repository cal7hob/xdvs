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
            Dispatcher.Subscribe(EventId.MainTankAppeared, Init);
            Dispatcher.Subscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, Init);
        Dispatcher.Unsubscribe(EventId.OnLanguageChange, OnQuestUpdated);
        Dispatcher.Unsubscribe(EventId.BattleQuestUpdated, OnQuestUpdated);
        Dispatcher.Unsubscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);
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
            Dispatcher.Subscribe(EventId.OnLanguageChange, OnQuestUpdated);
            Dispatcher.Subscribe(EventId.BattleQuestUpdated, OnQuestUpdated);
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


        int progress = 0;
        if (bs.quest.type == Quest.Type.Mileage)
            progress = ((int)BattleController.MyVehicle.Odometer);
        else if (bs.quest.type == Quest.Type.Revenge)
            progress = 0;//progress Not used
        else
            progress = BattleStatisticsManager.BattleStats[bs.quest.BattleStatsDictionaryKey];

        bool isQuestUpdated = progress != bs.quest.progress;
        bs.quest.progress = progress;
        if (isQuestUpdated) {
            OnQuestUpdated(EventId.BattleQuestUpdated, null);
            BattleStatisticsManager.WriteMileage();
        }
        if (bs.quest.CheckQuestComplete()) {
            sprComplete.SetActive(bs.quest.isComplete);
            Dispatcher.Send(EventId.QuestCompleted, new EventInfo_I((int)bs.quest.type));
        }
    }

    private void OnStatTableChangeVisibility(EventId id, EventInfo info)
    {
        SetActive(needToShow && !((EventInfo_B)info).bool1);
    }
}
