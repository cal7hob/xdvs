using Tanks.Models;
using UnityEngine;

public class ScoresMenuBehaviourClan : ScoresMenuBehaviour
{
    [SerializeField] private Clan scoresClan;

    public void ShowContextMenu(tk2dUIItem clickedUiItem, tk2dUILayout clickedItemsLayout, Clan clan)
    {
        scoresClan = clan;
        SetUpSpecialButtons();
        ShowContextMenu(clickedUiItem, clickedItemsLayout);
    }

    private void SetUpSpecialButtons()
    {
        if (ProfileInfo.Clan != null || scoresClan.MembersCount >= GameData.maxClanMembers || ProfileInfo.Level < GameData.accountManagementMinLevel)
            contextMenu.HideMenuItem("ClanJoin");
        else
            contextMenu.ShowMenuItem("ClanJoin");
        //Смотреть инфу о клане можно всем, поэтому убрал это условие(согласовав с Сегой)
        //if (ProfileInfo.Level < GameData.accountManagementMinLevel)
        //    contextMenu.HideMenuItem("ClanInfo");
    }

    private void ShowClanInfo(tk2dUIItem uiItem)
    {
        ClansManager.Instance.OpenParticularClanWebPage(scoresClan);
        contextMenu.HideContextMenu();
    }

    #region ClanApplyToJoin

    private void OnClanApplyToJoin(tk2dUIItem uiItem)
    {
        contextMenu.HideContextMenu();
        MessageBox.Show(MessageBox.Type.Question,
            Localizer.GetText("lblClanApplyToJoin", scoresClan.Name), answer =>
            {
                if (MessageBox.Answer.Yes == answer)
                    ClanApplyToJoin();
            });
    }

    private void ClanApplyToJoin()
    {
        var request = Http.Manager.Instance().CreateRequest("/player/addClanRequest");
        request.Form.AddField("clanId", scoresClan.Id);
        Http.Manager.StartAsyncRequest(request,
            delegate
            {
                MessageBox.Show(MessageBox.Type.Info,
                    Localizer.GetText("lblClanAppliedToJoin",
                    scoresClan.Name,
                    Clock.GetTimerString(GameData.chatModeratorAvailableBanTime, true)));
            });
    }

    #endregion
}
