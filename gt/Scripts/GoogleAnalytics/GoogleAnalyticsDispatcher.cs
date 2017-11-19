using System;
using System.Collections.Generic;
using GAEvent;
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

    private static event Action<EventChain> EventChainCompleted = delegate {};
    private static readonly Dictionary<Enum, EventChain> events = new Dictionary<Enum, EventChain>();

    void Awake()
    {
        Dispatcher.Subscribe(EventId.TutorialIndexChanged, OnTutorialIndexChanged);
        Dispatcher.Subscribe(EventId.BattleLessonAccomplished, OnBattleLessonAccomplished);
        Dispatcher.Subscribe(EventId.PlayerFled, OnPlayerFled);
        Dispatcher.Subscribe(EventId.BattleEnd, OnBattleEnd);
        Dispatcher.Subscribe(EventId.TroubleDisconnect, OnTroubleDisconnect);
        Dispatcher.Subscribe(EventId.PhotonDisconnectWithCause, OnPhotonDisconnect);
        Dispatcher.Subscribe(EventId.ProfileServerChoosed, OnProfileServerChoosed);
        Dispatcher.Subscribe(EventId.RewardedVideoClicked, OnRewardedVideoClicked);
        Dispatcher.Subscribe(EventId.ConsumableUsed, OnConsumableUsed);
        Dispatcher.Subscribe(EventId.ConsumableBought, OnConsumableBought);
        Dispatcher.Subscribe(EventId.VipConsumableClicked, OnVipConsumableClicked);
        Dispatcher.Subscribe(EventId.VipAccountPurchased, OnVipAccountPurchased);

        EventChainCompleted += OnEventChainCompleted;
        GUIPager.OnPageChange += OnPageChanged;
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TutorialIndexChanged, OnTutorialIndexChanged);
        Dispatcher.Unsubscribe(EventId.BattleLessonAccomplished, OnBattleLessonAccomplished);
        Dispatcher.Unsubscribe(EventId.PlayerFled, OnPlayerFled);
        Dispatcher.Unsubscribe(EventId.BattleEnd, OnBattleEnd);
        Dispatcher.Unsubscribe(EventId.TroubleDisconnect, OnTroubleDisconnect);
        Dispatcher.Unsubscribe(EventId.PhotonDisconnectWithCause, OnPhotonDisconnect);
        Dispatcher.Unsubscribe(EventId.ProfileServerChoosed, OnProfileServerChoosed);
        Dispatcher.Unsubscribe(EventId.RewardedVideoClicked, OnRewardedVideoClicked);
        Dispatcher.Unsubscribe(EventId.ConsumableUsed, OnConsumableUsed);
        Dispatcher.Unsubscribe(EventId.ConsumableBought, OnConsumableBought);
        Dispatcher.Unsubscribe(EventId.VipConsumableClicked, OnVipConsumableClicked);
        Dispatcher.Unsubscribe(EventId.VipAccountPurchased, OnVipAccountPurchased);

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

    private void OnVipConsumableClicked(EventId id, EventInfo ei)
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

    private void OnVipAccountPurchased(EventId id, EventInfo ei)
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
