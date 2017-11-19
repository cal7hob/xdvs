using System.Collections;

public abstract class BotState
{
    protected BotAI botAI;
    private VehicleController thisVehicle;

    protected BotState(BotAI botAI)
    {
        this.botAI = botAI;
        thisVehicle = botAI.ThisVehicle;
    }

    protected void StopAllBotCoroutines()
    {
        if (thisVehicle != null)
        {
            thisVehicle.StopAllCoroutines();
        }
    }

    public virtual void OnFinish()
    {
        StopAllBotCoroutines();
    }

    public virtual void OnStart()
    {
        botAI.OnStateChange(); 
        botAI.CurrentBehaviour.StartSettingHumanTargetPreference();
    }

    public abstract IEnumerator Updating(); 
}
