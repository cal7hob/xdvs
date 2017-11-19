using System;
using UnityEngine;

public class SocialUserInfoView : MonoBehaviour
{
	[SerializeField]
	private Avatar avatar;
	[SerializeField]
    private tk2dTextMesh nameLabel;

	private ISocialService socialService;

	public void Initialize(ISocialService socialService)
	{
        this.socialService = socialService;
        if (socialService.IsLoggedIn)
		{
			SetupView();
		}
	}

	private void SetupView()
	{
        var player = socialService.Player;
		if (player != null)
		{
			nameLabel.text = string.Format("{0} {1}",player.Social.FirstName,player.Social.LastName);
			avatar.Init(player);
            avatar.DownloadAvatar();
		}
	}
}
