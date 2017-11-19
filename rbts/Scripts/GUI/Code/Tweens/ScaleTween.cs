using UnityEngine;
using System.Collections;

/// <summary>
/// Will scale uiItem up and down, on press events
/// </summary>
public class ScaleTween : MonoBehaviour
{
    private Vector3 onUpScale; //keeps track of original scale

    /// <summary>
    /// What it should scsale to onDown event
    /// </summary>
    public Vector3 onDownScale = new Vector3(.9f, .9f, .9f);

    /// <summary>
    /// How long the tween (scaling) should last in seconds. If set to 0 no tween is used, happens instantly.
    /// </summary>
    public float tweenDuration = .1f;

    public float startDelay = 0f;

    private bool internalTweenInProgress = false;
    private Vector3 tweenTargetScale = Vector3.one;
    private Vector3 tweenStartingScale = Vector3.one;
    private float tweenTimeElapsed = 0;

    void Awake()
    {
        onUpScale = transform.localScale;
    }

    void OnEnable()
    {
        internalTweenInProgress = false;
        tweenTimeElapsed = 0;
        transform.localScale = onUpScale;
        ButtonDown();
    }

    void OnDisable()
    {
    }

    private void ButtonDown()
    {
        if (tweenDuration <= 0)
        {
            transform.localScale = onDownScale;
        }
        else
        {
            transform.localScale = onUpScale;

            tweenTargetScale = onDownScale;
            tweenStartingScale = transform.localScale;
            if (!internalTweenInProgress)
            {
                this.Invoke(StartAnimation, startDelay);
                //StartCoroutine(DoScaleTween());
                //internalTweenInProgress = true;
            }
        }
    }

    void StartAnimation ()
    {
        if (!gameObject.GetActive())
            return;

        StartCoroutine(DoScaleTween());
        internalTweenInProgress = true;
    }

    private IEnumerator DoScaleTween()
    {
        tweenTimeElapsed = 0;
        while (tweenTimeElapsed < tweenDuration)
        {
            transform.localScale = Vector3.Lerp(tweenStartingScale, tweenTargetScale, tweenTimeElapsed / tweenDuration);
            yield return null;
            tweenTimeElapsed += tk2dUITime.deltaTime;
        }
        transform.localScale = tweenTargetScale;
        internalTweenInProgress = false;

        if (tweenDuration <= 0)
        {
            transform.localScale = onUpScale;
        }
        else
        {
            tweenTargetScale = onUpScale;
            tweenStartingScale = transform.localScale;
            StartCoroutine(DoScaleTween());
            internalTweenInProgress = true;
        }
    }

}
