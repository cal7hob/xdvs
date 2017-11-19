using System;
using UnityEngine;

public sealed class SettingsSocialGroupItemView : MonoBehaviour
{
	[SerializeField]
    private tk2dUIItem joinGroupButton;

	[SerializeField]
	private GameObject joinGroupRoot;

	[SerializeField]
	private GameObject linkGroupRoot;

	[SerializeField]
    private tk2dUIItem linkButton;

	[SerializeField]
	private tk2dTextMesh[] nameLabels;

	[SerializeField]
	private tk2dBaseSprite[] socialNetworkIcons;

	private SocialNetworkGroupInfo socialNetworkGroupInfo;

	private SocialNetworkGroup socialNetworkGroup;
    
    private bool isMember;

    //public float ItemHeight//���� �� ��� ������ tk2dUILayout
    //{
    //    get
    //    {
    //        return (GetComponent<tk2dUILayout>().GetMaxBounds()
    //           - GetComponent<tk2dUILayout>().GetMinBounds()).y;
    //    }
    //}

    private void Awake()
	{
		joinGroupButton.OnClick += JoinGroupButtonClick;
		linkButton.OnClick += LinkButtonClick;
	}

	public void Initialize(SocialNetworkGroup socialNetworkGroup)
	{
		this.socialNetworkGroup = socialNetworkGroup;
		isMember = socialNetworkGroup.IsMember;
		socialNetworkGroupInfo = socialNetworkGroup.Info;
		SetupInfo(socialNetworkGroupInfo.PictureName);
	}

	private void SetupInfo(string networkLogoIcon)
	{
		joinGroupRoot.SetActive(!isMember);
		linkGroupRoot.SetActive(isMember);
        foreach (tk2dTextMesh label in nameLabels)
		{
		    label.text = socialNetworkGroupInfo.Name;
		}
        foreach (tk2dBaseSprite sprite in socialNetworkIcons)
		{
		    sprite.SetSprite(networkLogoIcon);
		}
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
		Application.OpenURL(socialNetworkGroupInfo.Url);
	}
}
