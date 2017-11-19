using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class TweenAlpha : TweenBase
{
    [Header("TweenAlpha variables")]
    public float minAlpha = 0;
    public float maxAlpha = 1;
    public tk2dBaseSprite[] objectsToAnimate;
    public bool stateOnAwake = false;
    [SerializeField] private float alphaInSecond = 1f;

    private List<float> objectsInitialAlpha = new List<float>();
    private float curAlpha = 0;
    private bool direction = true;
    private bool Direction
    {
        get { return direction; }
        set
        {
            direction = value;
        }
    }

    public override void OnInit()
    {
        if (objectsToAnimate != null)
            for (int i = 0; i < objectsToAnimate.Length; i++)
                objectsInitialAlpha.Add(objectsToAnimate[i].color.a);
    }

    public override void OnSetToInitialState()
    {
        if (objectsToAnimate != null)
            for (int i = 0; i < objectsToAnimate.Length; i++)
                objectsToAnimate[i].SetAlpha(objectsInitialAlpha[i]);
    }

    protected override IEnumerator Animate()
    {
        tweenInProgress = true;

        #region Механизм анимации

        Direction = true;
        curAlpha = minAlpha;
        MiscTools.SetSpritesAlpha(objectsToAnimate, curAlpha);
        while (tweenInProgress)
        {
            if(direction)
            {
                if (curAlpha >= maxAlpha)
                {
                    curAlpha = maxAlpha;
                    Direction = !Direction;
                }
                else
                {
                    curAlpha += alphaInSecond * Time.deltaTime;
                }
            }
            else
            {
                if (curAlpha <= minAlpha)
                {
                    curAlpha = minAlpha;
                    Direction = !Direction;
                }
                else
                {
                    curAlpha -= alphaInSecond * Time.deltaTime;
                }
            }
            MiscTools.SetSpritesAlpha(objectsToAnimate, curAlpha);
            yield return null;
        }
        #endregion

        if (setToInitialStateAtStop)
            OnSetToInitialState();
        tweenInProgress = false;//Переменная сбросится по Stop()
    }

    protected override bool IsParametersValid()
    {
        return minAlpha >= 0 && maxAlpha <= 1 && maxAlpha > minAlpha && !HelpTools.IsEmptyCollection(objectsToAnimate);
    }
}
