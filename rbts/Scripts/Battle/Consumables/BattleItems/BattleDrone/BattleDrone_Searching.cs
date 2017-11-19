using System.Collections;
using System.Collections.Generic;
using DemetriTools.Optimizations;
using UnityEngine;
using BattleDroneEnemySearching;
using StateMachines;

public class BattleDrone_Searching : BattleDroneState
{
    private readonly RepeatingOptimizer repeater;
    private readonly List<VehicleController> potentialTargets = new List<VehicleController>(10);

    public BattleDrone_Searching(BattleDrone owner, float targetSearchPeriod) : base(owner)
    {
        repeater = new RepeatingOptimizer(targetSearchPeriod);
    }

    public override string Name { get { return "SearchingTarget";} }

    public override void OnEnter(IState previousState)
    {
        repeater.Reset();
        Update();
    }

    public override void Update()
    {
        battleDrone.ReturnToNormalRotation();

        if (!repeater.AskPermission())
            return;

        VehicleController target = FindTarget();
        battleDrone.SetTarget(target);

        if (target != null)
        {
            battleDrone.SetState(ATTACK_ID);
        }
    }

    private VehicleController FindTarget()
    {
        potentialTargets.Clear();
        foreach (VehicleController veh in BattleController.allVehicles.Values)
        {
            if (
                veh == battleDrone.Owner
                || VehicleController.AreFriends(veh, battleDrone.Owner)
                || !battleDrone.CanCatch(veh))
            {
                continue;
            }

            potentialTargets.Add(veh);
        }

        float maxSuitability = float.NegativeInfinity;
        VehicleController target = null;
        SearchStrategy searchStrategy = battleDrone.SearchStrategy;
        foreach (var potTrg in potentialTargets)
        {
            float suitability = searchStrategy.GetSuitability(potTrg);
            if (suitability > maxSuitability)
            {
                maxSuitability = suitability;
                target = potTrg;
            }
        }

        return target;
    }
}
