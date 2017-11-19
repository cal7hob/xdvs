using System;
using UnityEngine;

public sealed class LinkAccountPageGroupItem : MonoBehaviour
{
	[SerializeField] private tk2dUIItem joinGroupButton;
	[SerializeField] private GameObject joinGroupRoot;
	[SerializeField] private GameObject linkGroupRoot;
	[SerializeField] private tk2dUIItem linkButton;
	[SerializeField] private tk2dTextMesh[] nameLabels;
	[SerializeField] private tk2dBaseSprite[] socialNetworkIcons;
    [SerializeField] private SocialNetworkIconType iconType = SocialNetworkIconType.socialButton;

	private SocialNetworkGroup socialNetworkGroup;
    private bool isMember;

    private void Awake()
	{
		joinGroupButton.OnClick += JoinGroupButtonClick;
		linkButton.OnClick += LinkButtonClick;
	}

	public void Initialize(SocialNetworkGroup socialNetworkGroup)
	{
		this.socialNetworkGroup = socialNetworkGroup;
		isMember = socialNetworkGroup.IsMember;

        joinGroupRoot.SetActive(!isMember);
        linkGroupRoot.SetActive(isMember);
        HelpTools.SetTextToAllLabelsInCollection(nameLabels, socialNetworkGroup.Info.Name);
        HelpTools.SetSpriteToAllSpritesInCollection(socialNetworkIcons, SocialSettings.GetSocialNetworkLogo(socialNetworkGroup.Info.Platform, iconType));
	}

	private void JoinGroupButtonClick()
	{
		socialNetworkGroup.Joined += SocialNetworkGroupJoined;
		socialNetworkGroup.Join();
	}

	private void SocialNetworkGroupJoined(string groupId)
	{
		socialNetworkGroup.Joined -= SocialNetworkGroupJoined;
		joinGroupRoot.SetActive(false);
		linkGroupRoot.SetActive(true);
	}

	private void LinkButtonClick()
	{
		Application.OpenURL(socialNetworkGroup.Info.Url);
	}
}
