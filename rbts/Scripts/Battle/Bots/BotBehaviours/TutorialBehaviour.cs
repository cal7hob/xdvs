using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

class TutorialBehaviour : TargetBehaviour
{
    public TutorialBehaviour(BotAI BotAi) : base(BotAi)
    {
    }

    public override void Apply()
    {
        Messenger.Subscribe(EventId.VehicleKilled, OnTankKilled);
    }

    public override void OnBotDestroy()
    {
        Messenger.Unsubscribe(EventId.VehicleKilled, OnTankKilled);
    }

    public override float FindingPosDelay { get { return BotSettings.findingPosDelaysTutorial_s.RandWithinRange; } }

    public override void FindTarget()
    {
        BotAI.Target = null;
    }

    private void OnTankKilled(EventId eid, EventInfo ei)
    {
        EventInfo_II info = ei as EventInfo_II;

        if (info.int1 != BotAI.ThisVehicle.data.playerId)
            return;

        CoroutineHelper.Start(TutorialBotExit(BotAI.ThisVehicle));
    }

    private static IEnumerator TutorialBotExit(VehicleController botVehicle)
    {
        yield return new WaitForSeconds(GameData.respawnTime);
        if (botVehicle != null)
            BotDispatcher.Instance.RemoveBot(botVehicle);
    }

    public override void OnDamage(int attackerId)
    { }
}
