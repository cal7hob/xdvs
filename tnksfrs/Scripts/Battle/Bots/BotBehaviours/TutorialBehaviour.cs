using System.Collections;
using UnityEngine;
using XD;

class TutorialBehaviour : TargetBehaviour
{
    protected IUnitBehaviour playerUnit = null;

    public TutorialBehaviour(BotAI BotAi) : base(BotAi)
    {
        playerUnit = StaticContainer.BattleController.CurrentUnit;
    }

    public override IEnumerator Shooting()
    {
        yield break;
    }

    public override IEnumerator FindingPosition()
    {
        while (playerUnit != null)
        {
            findingPosDelay = MiscTools.random.Next(3, 6);
            PositionToMove = playerUnit.Transform.position;

            yield return new WaitForSeconds(findingPosDelay);
        }
    }
}
