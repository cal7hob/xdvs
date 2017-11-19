using System.Collections;
using System.Collections.Generic;
using DemetriTools.Optimizations;
using StateMachines;
using UnityEngine;

public class BattleDrone_WaitTarget : BattleDroneState
{
    public override string Name { get { return "WaitingTarget"; } }

    private Timer timer;

    public BattleDrone_WaitTarget(BattleDrone owner, float timeOut) : base(owner)
    {
        timer = new Timer(timeOut);
    }

    public override void OnEnter(IState previousState)
    {
        timer.Start();
    }

    public override void Update()
    {
        if (battleDrone.HasAccessibleTarget())
        {
            battleDrone.SetState(ATTACK_ID);
            return;
        }

        if (timer.TimeElapsed)
        {
            //Время ожидания истекло. Меняем цель.
            battleDrone.SetState(SEARCHING_ID);
        }
    }
}
