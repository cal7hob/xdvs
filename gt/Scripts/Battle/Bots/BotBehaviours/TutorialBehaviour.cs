using System.Collections;
using UnityEngine;

public class TutorialBehaviour : TargetBehaviour
{
    public TutorialBehaviour(BotAI BotAi) : base(BotAi)
    {
        BotSettings =
            ScriptableObject.Instantiate(
                Resources.Load<BotSettings>(string.Format("{0}/ScriptableObjects/BotSettings/TutorialBotSettings", GameManager.CurrentResourcesFolder)));
    }

    public override IEnumerator Shoot()
    {
        yield break;
    }

    public override void FindTarget()
    {
        BotAI.Target = myVehicle;
    }
}
