using System.Collections;
using UnityEngine;

public class SkipLessonButton : MonoBehaviour
{
    public tk2dUIUpDownButton btnSkipLesson;
    public tk2dSlicedSprite sprButtonUp;
    public tk2dTextMesh lblSkipLesson;

    private const float APPEARANCE_SPEED = 1.33f;
    private const float DISAPPEARING_SPEED = 2.0f;
    private const float SHOW_DELAY = 15.0f;
    private const float HIDE_DELAY = 0.33f;

    private IEnumerator showingRoutine;
    private IEnumerator hidingRoutine;

    void Awake()
    {
        Dispatcher.Subscribe(EventId.BattleLessonStarted, OnBattleLessonStarted);
        Dispatcher.Subscribe(EventId.BattleTutorialSkipping, BattleTutorialSkipping);
        Dispatcher.Subscribe(EventId.BattleEnd, OnBattleEnd);
    }

    void OnDestroy()
    {
        if (showingRoutine != null)
            CoroutineHelper.Stop(showingRoutine);

        if (hidingRoutine != null)
            CoroutineHelper.Stop(hidingRoutine);

        Dispatcher.Unsubscribe(EventId.BattleLessonStarted, OnBattleLessonStarted);
        Dispatcher.Unsubscribe(EventId.BattleTutorialSkipping, BattleTutorialSkipping);
        Dispatcher.Unsubscribe(EventId.BattleEnd, OnBattleEnd);
    }

    private void OnBattleLessonStarted(EventId id, EventInfo ei)
    {   
        EventInfo_I info = (EventInfo_I)ei;

        BattleTutorial.BattleLessons battleLesson = (BattleTutorial.BattleLessons)info.int1;

        showingRoutine = Showing();

        if (battleLesson == BattleTutorial.BattleLessons.move)
            CoroutineHelper.Start(showingRoutine);
    }

    private void BattleTutorialSkipping(EventId id, EventInfo ei)
    {
        hidingRoutine = Hiding();
        CoroutineHelper.Start(hidingRoutine);
    }

    private IEnumerator Showing()
    {
        yield return new WaitForSeconds(SHOW_DELAY);

        if (btnSkipLesson == null)
            yield break;

        if (!btnSkipLesson.gameObject.activeSelf)
            btnSkipLesson.gameObject.SetActive(true);

        float alpha = 0;

        while (alpha < 1)
        {
            sprButtonUp.SetAlpha(alpha);
            lblSkipLesson.SetAlpha(alpha);

            alpha += APPEARANCE_SPEED * Time.deltaTime;

            yield return null;
        }
    }

    private IEnumerator Hiding()
    {
        yield return new WaitForSeconds(HIDE_DELAY);

        float alpha = sprButtonUp.color.a;

        while (alpha > 0)
        {
            sprButtonUp.SetAlpha(alpha);
            lblSkipLesson.SetAlpha(alpha);

            alpha -= DISAPPEARING_SPEED * Time.deltaTime;

            yield return null;
        }

        if (btnSkipLesson.gameObject.activeSelf)
            btnSkipLesson.gameObject.SetActive(false);
    }

    private void OnBattleEnd(EventId id, EventInfo info)
    {
        StopAllCoroutines();
    }
}
