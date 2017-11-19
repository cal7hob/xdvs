using System;
using UnityEngine;

public class SocialUserInfoView : MonoBehaviour
{
	[SerializeField]
	private Avatar avatar;

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
			avatar.Init(player);
            avatar.DownloadAvatar();
		}
	}
}
