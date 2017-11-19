using System;
using Tanks.Models;
using UnityEngine.SceneManagement;
using JSONObject = System.Collections.Generic.Dictionary<string, object>;

public class ScoresPagePlayers : ScoresPage
{
    public override void AddItem(JSONObject data)
    {
        var prefs = new JsonPrefs(data);
        
        var player = Player.Create(prefs);
        var place = prefs.ValueInt("place");
        var countryCode = prefs.ValueString("countryCode").ToLower();

        pageItems[player.Id] = 
            ScoresItem
            .Create(this, (ScoresItemPlayer)scoresItemPrefab)
            .Init(player, place, PlaceKey == "world" ? countryCode : "", scoresMenuBehaviour as ScoresMenuBehaviourPlayer);
    }

    public override void UpdatePlayer(Player player)
    {
        if (!pageItems.ContainsKey(player.Id))
            return;

        var item = (ScoresItemPlayer)pageItems[player.Id];
        item.UpdateData(player);
    }

    /// <summary>
    /// Используется для моментального обновления ника игрока в списках
    /// </summary>
    public void ChangePlayersNickName(EventId id, EventInfo info)
    {
        if (pageItems.ContainsKey(ProfileInfo.profileId) && pageItems[ProfileInfo.profileId] != null)
        {
            pageItems[ProfileInfo.profileId].UpdateNameLabel(ProfileInfo.PlayerName);
        }
    }

	protected override void Init() {
        base.Init();
        Dispatcher.Subscribe(EventId.NickNameChanged, ChangePlayersNickName);
        SceneManager.sceneUnloaded += OnSceneUnloaded;
	}

    protected override void OnSceneUnloaded(Scene scene)
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        base.OnSceneUnloaded(scene);
        Dispatcher.Unsubscribe(EventId.NickNameChanged, ChangePlayersNickName);
    }
}
