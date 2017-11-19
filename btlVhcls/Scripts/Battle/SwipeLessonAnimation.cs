using System.Collections;
using UnityEngine;

public class SwipeLessonAnimation : MonoBehaviour
{
    public tk2dSprite sprArrowLeft;
    public tk2dSprite sprArrowRight;
    public tk2dSprite sprHand;
    public tk2dSprite sprCircle;
    public Transform handWrapper;
    public GameObject wrapper;
    public float fadingSpeed = 0.1f;
    public float handFadingRatio = 0.5f;
    public float circleFadingRatio = 2.0f;
    public float moveSpeed = 1.25f;

    private const float THRESHOLD = 0.99f;
    private const float HAND_MOVE_LENGTH = 400.0f;

    private IEnumerator animationRoutine;

    void Awake()
    {
        Dispatcher.Subscribe(EventId.BattleTutorialSkipping, OnBattleTutorialSkipping);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.BattleTutorialSkipping, OnBattleTutorialSkipping);
    }

    public void Play()
    {
        wrapper.SetActive(true);

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = Animation();

        StartCoroutine(animationRoutine);
    }

    public void Stop()
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        wrapper.SetActive(false);
    }

    private void OnBattleTutorialSkipping(EventId id, EventInfo info)
    {
        Stop();
    }

    private IEnumerator Animation()
    {
        while (true)
        {
            while (sprArrowLeft.color.a < THRESHOLD)
            {
                sprArrowLeft.SetAlpha(Mathf.Lerp(sprArrowLeft.color.a, 1, fadingSpeed));
                sprArrowRight.SetAlpha(Mathf.Lerp(sprArrowRight.color.a, 1, fadingSpeed));

                yield return new WaitForEndOfFrame();
            }

            yield return new WaitForSeconds(0.5f);

            while (sprHand.color.a < THRESHOLD)
            {
                sprHand.SetAlpha(Mathf.Lerp(sprHand.color.a, 1, fadingSpeed * handFadingRatio));
                yield return new WaitForEndOfFrame();
            }

            yield return new WaitForSeconds(0.5f);

            while (sprCircle.color.a < THRESHOLD)
            {
                sprCircle.SetAlpha(Mathf.Lerp(sprCircle.color.a, 1, fadingSpeed * circleFadingRatio));
                yield return new WaitForEndOfFrame();
            }

            yield return new WaitForSeconds(0.5f);

            float handTargetX = handWrapper.localPosition.x + HAND_MOVE_LENGTH;

            while (handWrapper.localPosition.x < handTargetX)
            {
                handWrapper.Translate(Vector3.right * moveSpeed);
                yield return new WaitForEndOfFrame();
            }

            yield return new WaitForSeconds(0.5f);

            while (sprCircle.color.a > 1 - THRESHOLD)
            {
                sprCircle.SetAlpha(Mathf.Lerp(sprCircle.color.a, 0, fadingSpeed * circleFadingRatio));
                yield return new WaitForEndOfFrame();
            }

            while (sprHand.color.a > 1 - THRESHOLD)
            {
                sprHand.SetAlpha(Mathf.Lerp(sprHand.color.a, 0, fadingSpeed * handFadingRatio));
                yield return new WaitForEndOfFrame();
            }

            handWrapper.localPosition = Vector3.zero;
        }
    }
}
