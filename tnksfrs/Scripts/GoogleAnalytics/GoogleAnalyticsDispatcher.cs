using GAEvent;
using UnityEngine;
using XD;

public class GoogleAnalyticsDispatcher : MonoBehaviour // TODO: перенести потом все GoogleAnalyticsWrapper.LogEvent() сюда. 
{
    void Awake()
    {
        Dispatcher.Subscribe(EventId.TutorialIndexChanged, OnTutorialIndexChanged);
        Dispatcher.Subscribe(EventId.BattleLessonAccomplished, OnBattleLessonAccomplished);
        Dispatcher.Subscribe(EventId.PlayerFled, OnPlayerFled);
        Dispatcher.Subscribe(EventId.BattleEnd, OnBattleEnd);
        Dispatcher.Subscribe(EventId.TroubleDisconnect, OnTroubleDisconnect);
        Dispatcher.Subscribe(EventId.PhotonDisconnectWithCause, OnPhotonDisconnect);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TutorialIndexChanged, OnTutorialIndexChanged);
        Dispatcher.Unsubscribe(EventId.BattleLessonAccomplished, OnBattleLessonAccomplished);
        Dispatcher.Unsubscribe(EventId.PlayerFled, OnPlayerFled);
        Dispatcher.Unsubscribe(EventId.BattleEnd, OnBattleEnd);
        Dispatcher.Unsubscribe(EventId.TroubleDisconnect, OnTroubleDisconnect);
        Dispatcher.Unsubscribe(EventId.PhotonDisconnectWithCause, OnPhotonDisconnect);
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
                .SetValue(StaticType.Profile.Instance<IProfile>().LevelCalculator.Level));
    }

    private void OnBattleLessonAccomplished(EventId id, EventInfo ei)
    {
        BattleLessons battleLessonKey = (BattleLessons)((EventInfo_I)ei).int1;

        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(Category.BattleLessons)
                .SetParameter(Action.Completed)
                .SetParameter<Label>()
                .SetSubject(Subject.BattleLesson, battleLessonKey.ToFriendlyString())
                .SetValue(StaticType.Profile.Instance<IProfile>().LevelCalculator.Level));
    }

    private void OnPlayerFled(EventId id, EventInfo ei)
    {
        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(Category.LeaveBattle)
                .SetParameter(Action.LeftBattleManually)
                .SetParameter<Label>()
                .SetSubject(Subject.PlayerLevel, StaticType.Profile.Instance<IProfile>().LevelCalculator.Level));

        if (!StaticType.BattleTutorial.Instance<IBattleTutorial>().IsCompleted)
            GoogleAnalyticsWrapper.LogEvent(
                new CustomEventHitBuilder()
                    .SetParameter(Category.BattleLessons)
                    .SetParameter(Action.LeftBattleManually)
                    .SetParameter<Label>()
                    .SetSubject(Subject.BattleLesson, StaticType.BattleTutorial.Instance<IBattleTutorial>().CurrentBattleLesson.ToFriendlyString())
                    .SetValue(StaticType.Profile.Instance<IProfile>().LevelCalculator.Level));
    }


    private void OnBattleEnd(EventId id, EventInfo ei)
    {
        XD.EndBattleCause cause = (XD.EndBattleCause)((EventInfo_I)ei).int1;

        Action gaAction = Action.LeftBattleUnknownCause;

        switch (cause)
        {
            case XD.EndBattleCause.Timeouted:
                gaAction = Action.LeftBattleTimeouted;
                break;

            case XD.EndBattleCause.Inactivity:
                gaAction = Action.LeftBattleInactive;
                break;

            case XD.EndBattleCause.ApplicationPaused:
                gaAction = Action.LeftBattlePaused;
                break;

            case XD.EndBattleCause.AlreadyInBattle:
                gaAction = Action.LeftBattleForSecondEnter;
                break;

            case XD.EndBattleCause.FinishedTutorial:
                gaAction = Action.LeftBattleCompletedTutorial;
                break;
        }

        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(Category.LeaveBattle)
                .SetParameter(gaAction)
                .SetParameter<Label>()
                .SetSubject(Subject.PlayerLevel, StaticType.Profile.Instance<IProfile>().LevelCalculator.Level));

        if (!StaticType.Profile.Instance<IProfile>().BattleTutorialCompleted)
            GoogleAnalyticsWrapper.LogEvent(
                new CustomEventHitBuilder()
                    .SetParameter(Category.BattleLessons)
                    .SetParameter(gaAction)
                    .SetParameter<Label>()
                    .SetSubject(Subject.BattleLesson, StaticType.BattleTutorial.Instance<IBattleTutorial>().CurrentBattleLesson.ToFriendlyString())
                    .SetValue(StaticType.Profile.Instance<IProfile>().LevelCalculator.Level));
    }

    private void OnTroubleDisconnect(EventId id, EventInfo ei)
    {
        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(Category.LeaveBattle)
                .SetParameter(Action.LeftBattleDisconnected)
                .SetParameter<Label>()
                .SetSubject(Subject.PlayerLevel, StaticType.Profile.Instance<IProfile>().LevelCalculator.Level));
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
                .SetSubject(Subject.PlayerLevel, StaticType.Profile.Instance<IProfile>().LevelCalculator.Level));
    }
}
