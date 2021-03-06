﻿using System.Collections;
using UnityEngine;

class StopState : BotState
{
    public StopState(BotAI BotAi) : base(BotAi)
    {
    }

    public override void OnStart()
    {
        base.OnStart();

        botAI.GettingClosestBonusController.Pause();
        botAI.GettingClosestVehicleController.Pause();
        botAI.FindingOneShotEnemyController.Resume();
        botAI.CheckingIfStuckController.Pause();
        botAI.FindTargetController.Resume();
        botAI.FindingPositionController.Pause();
        botAI.PathUpdatingController.Pause();
        botAI.ShootingController.Resume();

        botAI.ResetUpdatingCoroutine(botAI.StopStateUpdating());
    }
}
