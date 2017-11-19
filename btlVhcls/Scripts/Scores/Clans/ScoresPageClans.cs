using Tanks.Models;
using UnityEngine;
using JSONObject = System.Collections.Generic.Dictionary<string, object>;

public class ScoresPageClans : ScoresPage
{
    public override void AddItem(JSONObject data)
    {
        var prefs = new JsonPrefs(data);

        var clan = Clan.Create(prefs);
        var place = prefs.ValueInt("place");

        pageItems[clan.Id] = ScoresItem
            .Create(this, (ScoresItemClan)scoresItemPrefab)
            .Init(clan, place, scoresMenuBehaviour as ScoresMenuBehaviourClan);
    }

    public override ScoresItem Reposition()
    {
        var clanPosition = base.Reposition();

        #region Adding CreateClan button to the clans list
        if (ProfileInfo.Clan != null || ProfileInfo.Level < GameData.accountManagementMinLevel)
            return clanPosition;

        var createClanItemPosition = pageItems.Count;

        var createClanItem = ScoresItem.Create(this, ScoresController.Instance.createClanItemPrefab);

        createClanItem.transform.localPosition = 
            new Vector3(0, -itemHeight * createClanItemPosition - (ScoresController.Instance.spaceBetweenItems * createClanItemPosition), 0);

        var createClanItemHeight = (createClanItem.GetComponent<tk2dUILayout>().GetMaxBounds()
                                    - createClanItem.GetComponent<tk2dUILayout>().GetMinBounds()).y;

        contentLength += createClanItemHeight + ScoresController.Instance.spaceBetweenItems;
        #endregion

        return clanPosition;
    }
}
