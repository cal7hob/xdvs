using System;
using UnityEngine;

public class PeriodicTask
{
    private float timer;
    private float delay;
    private Action callback;
    private ParamsRange range;

    public PeriodicTask(Action callback, float delay)
    {
        this.callback = callback;
        this.delay = delay;
        Restart();
    }

    public PeriodicTask(Action callback, ParamsRange range)
    {
        this.callback = callback;
        this.range = range;
        delay = range.RandWithinRange;
        Restart();
    }

    public void Restart()
    {
        timer = delay;
    }

    public void TryExecute(bool randomizeDelay = false)
    {
        timer += Time.deltaTime;

        if (timer > delay)
        {
            Execute();

            if (randomizeDelay)
            {
                delay = range.RandWithinRange;
            }
        }
    }

    public void Execute()
    {
        callback.Invoke();
        timer = 0;
    }
}