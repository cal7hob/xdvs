using System.Collections;
using UnityEngine;

public class SkipLessonButton : MonoBehaviour
{
    public tk2dUIUpDownButton btnSkipLesson;
    public GameObject[] objectsToChangeAlpha;

    private const float APPEARANCE_SPEED = 1.33f;
    private const float DISAPPEARING_SPEED = 2.0f;
    private const float HIDE_DELAY = 0.33f;

    private float alpha;

    private IEnumerator showingRoutine;
    private IEnumerator hidingRoutine;

    void Awake()
    {
        Messenger.Subscribe(EventId.BattleGUIIntialized, OnBattleGUIIntialized);
        Messenger.Subscribe(EventId.BattleTutorialSkipping, BattleTutorialSkipping);
        Messenger.Subscribe(EventId.BattleEnd, OnBattleEnd);
    }

    void OnDestroy()
    {
        if (showingRoutine != null)
            CoroutineHelper.Stop(showingRoutine);

        if (hidingRoutine != null)
            CoroutineHelper.Stop(hidingRoutine);

        Messenger.Unsubscribe(EventId.BattleGUIIntialized, OnBattleGUIIntialized);
        Messenger.Unsubscribe(EventId.BattleTutorialSkipping, BattleTutorialSkipping);
        Messenger.Unsubscribe(EventId.BattleEnd, OnBattleEnd);
    }

    private void OnBattleGUIIntialized(EventId id, EventInfo ei)
    {   
        showingRoutine = Showing();
        CoroutineHelper.Start(showingRoutine);
    }

    private void BattleTutorialSkipping(EventId id, EventInfo ei)
    {
        hidingRoutine = Hiding();
        CoroutineHelper.Start(hidingRoutine);
    }

    private IEnumerator Showing()
    {
        if (btnSkipLesson == null)
            yield break;

        if (!btnSkipLesson.gameObject.activeSelf)
            btnSkipLesson.gameObject.SetActive(true);

        alpha = 0;

        while (alpha < 1)
        {
            if (objectsToChangeAlpha != null)
            {
                foreach (var objectToChangeAlpha in objectsToChangeAlpha)
                    HelpTools.SetAlphaForAllWidgets(objectToChangeAlpha, alpha);
            }

            alpha += APPEARANCE_SPEED * Time.deltaTime;

            yield return null;
        }
    }

    private IEnumerator Hiding()
    {
        yield return new WaitForSeconds(HIDE_DELAY);

        while (alpha > 0)
        {
            foreach (var objectToChangeAlpha in objectsToChangeAlpha)
                HelpTools.SetAlphaForAllWidgets(objectToChangeAlpha, alpha);

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
