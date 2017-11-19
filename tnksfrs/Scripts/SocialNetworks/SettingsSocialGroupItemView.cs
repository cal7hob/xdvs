using System;
using UnityEngine;

public sealed class SettingsSocialGroupItemView : MonoBehaviour
{
	[SerializeField]
	private GameObject joinGroupRoot;

	[SerializeField]
	private GameObject linkGroupRoot;

	[SerializeField]
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
