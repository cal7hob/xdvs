using UnityEngine;
using System.Collections;

public class InviteItem : MonoBehaviour {
	public tk2dBaseSprite platformLogo;
	public tk2dTextMesh bonusValue;
	//[InspectorComment("Выбранные объекты скрываются если бонус за приглашение недоступен")]
	public GameObject[] bonusObjects;

	void OnEnable (){
        platformLogo.SetSprite(SocialSettings.GetCurPlatformSocialNetworkLogo(SocialNetworkIconType.socialButton));
		SocialSettings.OnLastInviteChanged += LastFriendInviteChanged;
    	bonusValue.text = "+"+ProfileInfo.MaxFuel;
    	LastFriendInviteChanged ();
    }
    
    void OnDisable () {
		SocialSettings.OnLastInviteChanged -= LastFriendInviteChanged;
    }
	
	void LastFriendInviteChanged () {
		bool isAvail = false;
		isAvail = SocialSettings.IsBonusForInviteAvailable;
    	foreach (var o in bonusObjects) {
    		o.SetActive (isAvail);
    	}
	}
	
	void InviteClicked () {
		SocialSettings.GetSocialService().InviteFriend();
	}
}
