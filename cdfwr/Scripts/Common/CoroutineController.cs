﻿using System;
using System.Collections;
using UnityEngine;

public enum CoroutineState
{
    Ready,
    Running,
    Paused,
    Finished
}

public class CoroutineController
{
    private IEnumerator routine;
    private MonoBehaviour monoBehaviour;

    public Coroutine ThisCoroutine { get; set; }
    public CoroutineState state;

    public CoroutineController(IEnumerator routine, MonoBehaviour monoBehaviour)
    {
        this.routine = routine;
        this.monoBehaviour = monoBehaviour;

        state = CoroutineState.Ready;
    }

    public IEnumerator Start()
    {
        if (state != CoroutineState.Ready)
        {
            throw new InvalidOperationException(string.Format("Unable to start coroutine in state: {0}", state));
        }

        state = CoroutineState.Running;
       
        while (routine.MoveNext() && routine != null)
        {
            yield return routine.Current;
            while (state == CoroutineState.Paused)
            {
                yield return null;
            }
            if (state == CoroutineState.Finished)
            {
                yield break;
            }
        }

        state = CoroutineState.Finished;
    }

    public void Restart()
    {
        if (ThisCoroutine != null)
        {
            monoBehaviour.StopCoroutine(ThisCoroutine);
        }

        state = CoroutineState.Ready;
        ThisCoroutine = monoBehaviour.StartCoroutine(Start());
    }

    public void Stop()
    {
        state = CoroutineState.Finished;
    }

    public void Pause()
    {
        state = CoroutineState.Paused;
    }

    public void Resume()
    {
        state = CoroutineState.Running;
    }

    public void TryExecute(Action act)
    {
        try
        {
            act.Invoke();
        }
        catch (Exception exception)
        {
            Debug.LogFormat(string.Format("coroutine {0} has been stopped cuz of the following exception: \"{1}\"", routine, exception.Message));
            routine = null;
        }
    }
}