using UnityEngine;
using System.Collections;

public class Module_TeamCount : InterfaceModuleBase
{
    [SerializeField] private tk2dTextMesh[] ourTeamCountLabels;
    [SerializeField] private tk2dTextMesh[] enemyTeamCountLabels;
    [SerializeField] private bool hideIfStatTableAppears = false;

    /// <summary>
    /// Используем Start вместо Awake чтобы BattleController.Instance уже был определен
    /// </summary>
    protected override void Start()
    {
        base.Start();
        if (!ProfileInfo.IsBattleTutorialCompleted)
            return;
        Messenger.Subscribe(EventId.TeamScoreChanged, TeamScoreChanged);
        Messenger.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        if (BattleController.Instance.BattleMode == GameData.GameMode.Team && hideIfStatTableAppears)
            Messenger.Subscribe(EventId.StatTableVisibilityChange, OnStatTableChangeVisibility);
        TeamScoreChanged(0,null);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Messenger.Unsubscribe(EventId.TeamScoreChanged, TeamScoreChanged);
        Messenger.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Messenger.Unsubscribe(EventId.StatTableVisibilityChange, OnStatTableChangeVisibility);
    }

    private void OnMainTankAppeared(EventId id, EventInfo info)
    {
        SetActive(BattleController.Instance.BattleMode == GameData.GameMode.Team);
    }

    private void TeamScoreChanged(EventId id, EventInfo info)
    {
        HelpTools.SetTextToAllLabelsInCollection(ourTeamCountLabels, ScoreCounter.FriendTeamScore.ToString());
        HelpTools.SetTextToAllLabelsInCollection(enemyTeamCountLabels, ScoreCounter.EnemyTeamScore.ToString());
    }

    private void OnStatTableChangeVisibility(EventId id, EventInfo info)
    {
        SetActive(!((EventInfo_B)info).bool1);
    }
}
