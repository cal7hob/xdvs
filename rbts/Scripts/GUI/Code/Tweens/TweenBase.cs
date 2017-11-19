using UnityEngine;
using System.Collections;
using System;

public interface Tween
{
    void Play();
    void Stop();
    void OnInit();
    void OnSetToInitialState();
}

public class TweenBase : MonoBehaviour, Tween
{
    [Header("TweenBase variables")]
    public bool restartIfInProgress = false;//Если true - то про попытке запустить повторно проигрываемую в данный момент анимацию - начинаем сначала, иначе игнорируем
    public bool setToInitialStateAtStop = true;//Execute or not function SetToInitialState() at Stop()

    public bool IsTweenInProgress { get { return tweenInProgress; } }
    protected Coroutine animateCoroutine;
    private bool isInited = false;
    protected bool tweenInProgress = false;

    private void Awake()
    {
        Init();
    }

    public virtual void Play()
    {
        if (!gameObject.activeInHierarchy || !isInited || !IsParametersValid() || (IsTweenInProgress && !restartIfInProgress))
            return;
        Stop();
        animateCoroutine = StartCoroutine(Animate());
    }

    public virtual void Stop()
    {
        if (!isInited)
            return;
        tweenInProgress = false;
        if (animateCoroutine != null)
            StopCoroutine(animateCoroutine);
        if(setToInitialStateAtStop)
            SetToInitialState();
    }

    public void SetActiveAnimation(bool en)
    {
        if (en)
            Play();
        else
            Stop();
    }

    private void OnEnable()
    {
        SetToInitialState();
    }

    private void OnDisable()
    {
        Stop();
    }

    /// <summary>
    /// Сохранение старотового состояния при первой активации объекта - в перовм OnEnable
    /// </summary>
    public virtual void OnInit(){}

    private void Init()
    {
        if (isInited)
            return;
        OnInit();
        isInited = true;
    }

    private void SetToInitialState()
    {
        if (!isInited)
            return;
        OnSetToInitialState();
    }

    /// <summary>
    /// Возвращаем объекты в начальное состояние (которое сохранилось в Awake)
    /// </summary>
    public virtual void OnSetToInitialState(){}

    protected virtual IEnumerator Animate()
    {
        tweenInProgress = true;
        yield return null;
        tweenInProgress = false;
        yield break;
    }

    protected virtual bool IsParametersValid()
    {
        return true;
    }
}
