using UnityEngine;
using System.Collections;



public class TweenScale : TweenBase
{
    public enum AnimationType
    {
        Wow,                //Скейл до указанного и обратно в начальное состояние
    }

    public AnimationType animationType = AnimationType.Wow;
    public Vector3 tweenScale = new Vector3(1.2f, 1.2f, 1);
    /// <summary>
    /// How long the tween (scaling) should last in seconds. If set to 0 no tween is used, happens instantly.
    /// </summary>
    public float tweenDuration = 0.5f;

    private Vector3 startScale; //original scale
    private float tweenTimeElapsed = 0;
    private float tweenHalfDuration;

    public override void OnInit()
    {
        startScale = transform.localScale;
        tweenHalfDuration = tweenDuration / 2f;
    }

    public override void OnSetToInitialState()
    {
        transform.localScale = startScale;
    }

    protected override IEnumerator Animate()
    {
        tweenInProgress = true;

        #region Механизм анимации
        tweenTimeElapsed = 0;

        switch (animationType)
        {
            case AnimationType.Wow:
                while (tweenInProgress && tweenTimeElapsed < tweenHalfDuration)
                {
                    transform.localScale = Vector3.Lerp(startScale, tweenScale, tweenTimeElapsed / tweenHalfDuration);
                    yield return null;
                    tweenTimeElapsed += Time.deltaTime;
                }
                tweenTimeElapsed = 0;
                while (tweenInProgress && tweenTimeElapsed < tweenHalfDuration)
                {
                    transform.localScale = Vector3.Lerp(tweenScale, startScale, tweenTimeElapsed / tweenHalfDuration);
                    yield return null;
                    tweenTimeElapsed += Time.deltaTime;
                }
                break;
        }
        #endregion

        if (setToInitialStateAtStop)
            OnSetToInitialState();
        tweenInProgress = false;
    }

    protected override bool IsParametersValid()
    {
        return tweenDuration > 0;
    }

}
