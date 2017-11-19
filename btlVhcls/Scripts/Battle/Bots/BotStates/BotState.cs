using System.Collections;
using UnityEngine;

public abstract class BotState
{
    protected BotAI botAI;
    protected VehicleController thisVehicle;

    protected BotState(BotAI botAI)
    {
        this.botAI = botAI;
        thisVehicle = botAI.ThisVehicle;
    }

    public virtual void OnStart()
    {
        botAI.OnStateChange();
    }
}
