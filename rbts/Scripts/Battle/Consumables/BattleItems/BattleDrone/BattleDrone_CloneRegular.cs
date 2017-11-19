using System.Collections;
using System.Collections.Generic;
using DemetriTools.Optimizations;
using StateMachines;
using UnityEngine;

public class BattleDrone_CloneRegular : BattleDroneState
{
    public override string Name { get { return "CloneRegular"; } }

    private readonly RepeatingOptimizer reloader;
    private readonly RepeatingOptimizer visibilityRefresher;
    
    public BattleDrone_CloneRegular(BattleDrone battleDrone, float reloadTime) : base(battleDrone)
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
        if (
            battleDrone.Target == null
            || !battleDrone.Target.IsAvailable
            || (visibilityRefresher.AskPermission() && !battleDrone.HasAccessibleTarget())
            )
        {
            return;
        }
        
        if (battleDrone.RotateToPoint(battleDrone.Target.transform.position) && reloader.AskPermission())
        {
            battleDrone.Attack();
        }
    }
}
