using UnityEngine;
using System.Collections;



public class TweenBlink : TweenBase
{
    [Header("TweenBlink variables")]
    public float showingTime = 0.4f;
    public float pauseTime = 0.4f;
    public GameObject[] objectsToAnimate;
    public bool stateOnAwake = false;

    private float showingTimeCounter = 0.4f;
    private float pauseTimeCounter = 0.4f;
    private bool state = true;
    private bool State
    {
        get { return state; }
        set
        {
            state = value;
            MiscTools.SetObjectsActivity(objectsToAnimate, state);
        }
    }

    public override void OnInit()
    {
        State = stateOnAwake;
    }

    public override void OnSetToInitialState()
    {
        State = stateOnAwake;
    }

    protected override IEnumerator Animate()
    {
        tweenInProgress = true;

        #region Механизм анимации

        showingTimeCounter = 0;
        pauseTimeCounter = 0;
        State = true;
        while (tweenInProgress)
        {
            if(state)
            {
                if (showingTimeCounter >= showingTime)
                {
                    showingTimeCounter = 0;
                    State = !State;
                }
                else
                {
                    showingTimeCounter += Time.deltaTime;
                }
            }
            else
            {
                if (pauseTimeCounter >= pauseTime)
                {
                    pauseTimeCounter = 0;
                    State = !State;
                }
                else
                {
                    pauseTimeCounter += Time.deltaTime;
                }
            }

            yield return null;
        }
        #endregion

        if (setToInitialStateAtStop)
            OnSetToInitialState();
        tweenInProgress = false;//Переменная сбросится по Stop()
    }

    protected override bool IsParametersValid()
    {
        return showingTime > 0 && pauseTime > 0 && !HelpTools.IsEmptyCollection(objectsToAnimate);
    }
}
