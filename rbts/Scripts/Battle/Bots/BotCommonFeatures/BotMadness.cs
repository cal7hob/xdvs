using System.Collections;
using System.Collections.Generic;
using DemetriTools.Optimizations;
using UnityEngine;

public class BotMadness
{
    private RepeatingOptimizer horizRepeater;
    private RepeatingOptimizer vertRepeater;
    private RepeatingOptimizer fireRepeater;

    private float currentHoriz = 1f;
    private float currentVert = 1f;
    private bool fireStatus;

    private System.Random random;
    private int sharpTurnProbability;
    private int gasPedalProbability;
    
    public BotMadness()
    {
        random = MiscTools.random;

        sharpTurnProbability = random.Next(20, 50);
        gasPedalProbability = 20;

        horizRepeater = new RepeatingOptimizer(Mathf.Clamp((float)random.NextDouble(), 0.5f, 2f));
        vertRepeater = new RepeatingOptimizer(3f);
        fireRepeater = new RepeatingOptimizer(0.1f + 3f * (float)random.NextDouble());
        fireStatus = random.NextDouble() > 0.5;
    }

    public float GetHorizAxis()
    {
        if (horizRepeater.AskPermission() && Pass(sharpTurnProbability))
        {
            currentHoriz = -currentHoriz;
        }

        return currentHoriz;
    }

    public float GetVertAxis()
    {
        if (vertRepeater.AskPermission())
        {
            currentVert = Pass(gasPedalProbability) ? 1f : 0f;
        }

        return currentVert;
    }

    public bool GetFireStatus()
    {
        if (fireRepeater.AskPermission())
        {
            fireStatus = !fireStatus;
            fireRepeater.Reset(0.5f + 3f * (float) random.NextDouble());
        }

        return fireStatus;
    }

    private bool Pass(int probability)
    {
        return MiscTools.random.Next(1000) % 100 < probability;
    }
}
