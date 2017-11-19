using System.Collections;
using System.Collections.Generic;
using DemetriTools.Optimizations;
using StateMachines;
using UnityEngine;

public class BattleDrone_Attack : BattleDroneState
{
    public override string Name { get { return "Attack"; } }
    private readonly RepeatingOptimizer reloader;
    private readonly RepeatingOptimizer visibilityRefresher;

    public BattleDrone_Attack(BattleDrone owner, float reloadTime) : base(owner)
    {
        reloader = new RepeatingOptimizer(reloadTime);
        visibilityRefresher = new RepeatingOptimizer(0.2f);
    }

    public override void OnEnter(IState prevState)
    {
        visibilityRefresher.Reset(); //Первая проверка сразу
        Update();
        visibilityRefresher.Reset((float)MiscTools.random.NextDouble() * 0.15f);
    }

    public override void Update()
    {
        if (battleDrone.Target == null || !battleDrone.Target.IsAvailable)
        {
            battleDrone.SetState(SEARCHING_ID);
            return;
        }

        if (visibilityRefresher.AskPermission() && !battleDrone.HasAccessibleTarget())
        {
            battleDrone.SetState(WAITING_TARGET_ID);
            return;
        }

        if (battleDrone.RotateToPoint(battleDrone.Target.transform.position) && reloader.AskPermission())
        {
            battleDrone.Attack();
        }
    }
}
