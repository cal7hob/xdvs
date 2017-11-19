using GAEvent;
using System;
using System.Collections.Generic;
using UnityEngine;
using Action = GAEvent.Action;

public class GoogleAnalyticsDispatcher : MonoBehaviour // TODO: перенести потом все GoogleAnalyticsWrapper.LogEvent() сюда. 
{
    class EventChain
    {
        internal readonly Enum key;
        internal readonly object[] args;

        public EventChain(Enum key, params object[] args)
        {
            this.key = key;
            this.args = args;
        }
    }

    private static event Action<EventChain> EventChainCompleted = delegate { };
    private static readonly Dictionary<Enum, EventChain> events = new Dictionary<Enum, EventChain>();

    void Awake()
    {
        Messenger.Subscribe(EventId.TutorialIndexChanged, OnTutorialIndexChanged);
        Messenger.Subscribe(EventId.BattleLessonAccomplished, OnBattleLessonAccomplished);
        Messenger.Subscribe(EventId.PlayerFled, OnPlayerFled);
        Messenger.Subscribe(EventId.BattleEnd, OnBattleEnd);
        Messenger.Subscribe(EventId.TroubleDisconnect, OnTroubleDisconnect);
        Messenger.Subscribe(EventId.PhotonDisconnectWithCause, OnPhotonDisconnect);
        Messenger.Subscribe(EventId.ProfileServerChoosed, OnProfileServerChoosed);
        Messenger.Subscribe(EventId.RewardedVideoClicked, OnRewardedVideoClicked);
        Messenger.Subscribe(EventId.ConsumableUsed, OnConsumableUsed);
        Messenger.Subscribe(EventId.ConsumableBought, OnConsumableBought);
        Messenger.Subscribe(EventId.VIPConsumableClicked, OnVIPConsumableClicked);
        Messenger.Subscribe(EventId.VIPAccountPurchased, OnVIPAccountPurchased);

        EventChainCompleted += OnEventChainCompleted;
        GUIPager.OnPageChange += OnPageChanged;
    }

    void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.TutorialIndexChanged, OnTutorialIndexChanged);
        Messenger.Unsubscribe(EventId.BattleLessonAccomplished, OnBattleLessonAccomplished);
        Messenger.Unsubscribe(EventId.PlayerFled, OnPlayerFled);
        Messenger.Unsubscribe(EventId.BattleEnd, OnBattleEnd);
        Messenger.Unsubscribe(EventId.TroubleDisconnect, OnTroubleDisconnect);
        Messenger.Unsubscribe(EventId.PhotonDisconnectWithCause, OnPhotonDisconnect);
        Messenger.Unsubscribe(EventId.ProfileServerChoosed, OnProfileServerChoosed);
        Messenger.Unsubscribe(EventId.RewardedVideoClicked, OnRewardedVideoClicked);
        Messenger.Unsubscribe(EventId.ConsumableUsed, OnConsumableUsed);
        Messenger.Unsubscribe(EventId.ConsumableBought, OnConsumableBought);
        Messenger.Unsubscribe(EventId.VIPConsumableClicked, OnVIPConsumableClicked);
        Messenger.Unsubscribe(EventId.VIPAccountPurchased, OnVIPAccountPurchased);

        EventChainCompleted -= OnEventChainCompleted;
        GUIPager.OnPageChange -= OnPageChanged;
    }

    public static void StartEventChain(Enum eventKey, params object[] args)
    {
        if (!events.ContainsKey(eventKey))
            events.Add(eventKey, new EventChain(eventKey, args));
    }

    public static void BreakEventChain(Enum eventKey)
    {
        if (events.ContainsKey(eventKey))
            events.Remove(eventKey);
    }

    public static void TryFinalizeEventChain(Enum eventKey)
    {
        if (!events.ContainsKey(eventKey))
            return;

        EventChainCompleted(events[eventKey]);

        events.Remove(eventKey);
    }

    private void OnTutorialIndexChanged(EventId id, EventInfo ei)
    {
        Tutorials tutorialKey = (Tutorials)((EventInfo_I)ei).int1;

        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(Category.Tutorials)
                .SetParameter(Action.Completed)
                .SetParameter<Label>()
                .SetSubject(Subject.Tutorial, tutorialKey.ToFriendlyString())
                .SetValue(ProfileInfo.Level));
    }

    private void OnBattleLessonAccomplished(EventId id, EventInfo ei)
    {
        BattleTutorial.BattleLessons battleLessonKey = (BattleTutorial.BattleLessons)((EventInfo_I)ei).int1;

        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(Category.BattleLessons)
                .SetParameter(Action.Completed)
                .SetParameter<Label>()
                .SetSubject(Subject.BattleLesson, battleLessonKey.ToFriendlyString())
                .SetValue(ProfileInfo.Level));
    }

    private void OnPlayerFled(EventId id, EventInfo ei)
    {
        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(Category.LeaveBattle)
                .SetParameter(Action.LeftBattleManually)
                .SetParameter<Label>()
                .SetSubject(Subject.PlayerLevel, ProfileInfo.Level));

        if (BattleTutorial.Instance != null && !BattleTutorial.IsCompleted)
            GoogleAnalyticsWrapper.LogEvent(
                new CustomEventHitBuilder()
                    .SetParameter(Category.BattleLessons)
                    .SetParameter(Action.LeftBattleManually)
                    .SetParameter<Label>()
                    .SetSubject(Subject.BattleLesson, BattleTutorial.Instance.CurrentBattleLesson.ToFriendlyString())
                    .SetValue(ProfileInfo.Level));
    }


    private void OnBattleEnd(EventId id, EventInfo ei)
    {
        BattleController.EndBattleCause cause = (BattleController.EndBattleCause)((EventInfo_I)ei).int1;

        Action gaAction = Action.LeftBattleUnknownCause;

        switch (cause)
        {
            case BattleController.EndBattleCause.Timeouted:
                gaAction = Action.LeftBattleTimeouted;
                break;

            case BattleController.EndBattleCause.Inactivity:
                gaAction = Action.LeftBattleInactive;
                break;

            case BattleController.EndBattleCause.ApplicationPaused:
                gaAction = Action.LeftBattlePaused;
                break;

            case BattleController.EndBattleCause.AlreadyInBattle:
                gaAction = Action.LeftBattleForSecondEnter;
                break;

            case BattleController.EndBattleCause.FinishedTutorial:
                gaAction = Action.LeftBattleCompletedTutorial;
                break;
        }

        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(Category.LeaveBattle)
                .SetParameter(gaAction)
                .SetParameter<Label>()
                .SetSubject(Subject.PlayerLevel, ProfileInfo.Level));

        if (!BattleTutorial.IsCompleted)
            GoogleAnalyticsWrapper.LogEvent(
                new CustomEventHitBuilder()
                    .SetParameter(Category.BattleLessons)
                    .SetParameter(gaAction)
                    .SetParameter<Label>()
                    .SetSubject(Subject.BattleLesson, BattleTutorial.Instance.CurrentBattleLesson.ToFriendlyString())
                    .SetValue(ProfileInfo.Level));
    }

    private void OnTroubleDisconnect(EventId id, EventInfo ei)
    {
        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(Category.LeaveBattle)
                .SetParameter(Action.LeftBattleDisconnected)
                .SetParameter<Label>()
                .SetSubject(Subject.PlayerLevel, ProfileInfo.Level));
    }

    private void OnPhotonDisconnect(EventId id, EventInfo ei)
    {
        EventInfo_S info = (EventInfo_S)ei;

        string cause = info.str1;

        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(Category.PhotonDisconnect)
                .SetParameter<Action>()
                .SetSubject(Subject.PhotonDisconnectCause, cause)
                .SetParameter<Label>()
                .SetSubject(Subject.PlayerLevel, ProfileInfo.Level));
    }

    private void OnProfileServerChoosed(EventId id, EventInfo ei)
    {
        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(Category.ChooseServer)
                .SetParameter<Action>()
                .SetSubject(Subject.Region, Http.Manager.Instance().Region)
                .SetParameter<Label>()
                .SetSubject(Subject.PlayerLevel, ProfileInfo.Level));
    }

    private void OnRewardedVideoClicked(EventId id, EventInfo ei)
    {
        RewardedVideoController.State state = (RewardedVideoController.State)((EventInfo_I)ei).int1;

        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(Category.RewardedVideo)
                .SetParameter(Action.Clicked)
                .SetParameter<Label>()
                .SetSubject(Subject.State, state.ToFriendlyString())
                .SetValue(ProfileInfo.Level));
    }

    private void OnConsumableUsed(EventId id, EventInfo ei)
    {
        EventInfo_II info = (EventInfo_II)ei;
        int playerId = info.int1;
        int consumableId = info.int2;

        if (playerId != BattleController.MyPlayerId)
            return;

        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(Category.ConsumableUsage)
                .SetParameter<Action>()
                .SetSubject(Subject.ConsumableID, consumableId)
                .SetParameter<Label>()
                .SetSubject(Subject.GameMode, GameData.Mode)
                .SetValue(ProfileInfo.Level));

        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(Category.ConsumableUsage)
                .SetParameter<Action>()
                .SetSubject(Subject.ConsumableID, consumableId)
                .SetParameter<Label>()
                .SetSubject(Subject.MapName, GameManager.CurrentMap)
                .SetValue(ProfileInfo.Level));
    }

    private void OnConsumableBought(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;
        int consumableId = info.int1;

        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(Category.ConsumableBuying)
                .SetParameter<Action>()
                .SetSubject(Subject.ConsumableID, consumableId)
                .SetParameter<Label>()
                .SetSubject(Subject.PlayerLevel, ProfileInfo.Level));
    }

    private void OnVIPConsumableClicked(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;
        int consumableId = info.int1;

        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(Category.VIPConsumableClicked)
                .SetParameter<Action>()
                .SetSubject(Subject.ConsumableID, consumableId)
                .SetParameter<Label>()
                .SetSubject(Subject.PlayerLevel, ProfileInfo.Level));
    }

    private void OnVIPAccountPurchased(EventId id, EventInfo ei)
    {
        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(Category.VIPAccountBuying)
                .SetParameter(Action.Bought)
                .SetParameter<Label>()
                .SetSubject(Subject.PlayerLevel, ProfileInfo.Level));

        TryFinalizeEventChain(Category.VIPAccountBoughtAfterVIPConsumableClicked);
    }

    private void OnEventChainCompleted(EventChain eventChain)
    {
        if (eventChain.key.ToString() == Category.VIPAccountBoughtAfterVIPConsumableClicked.ToString())
        {
            GoogleAnalyticsWrapper.LogEvent(
                new CustomEventHitBuilder()
                    .SetParameter(Category.VIPAccountBoughtAfterVIPConsumableClicked)
                    .SetParameter<Action>()
                    .SetSubject(Subject.ConsumableID, eventChain.args[0].ToString())
                    .SetParameter<Label>()
                    .SetSubject(Subject.PlayerLevel, ProfileInfo.Level));

            return;
        }
    }

    private void OnPageChanged(string fromPage, string toPage)
    {
        if (fromPage == "VipAccountShop" && !ProfileInfo.IsPlayerVip)
            BreakEventChain(Category.VIPAccountBoughtAfterVIPConsumableClicked);
    }
}
