using Tanks.Models;
using UnityEngine;
using System.Collections;
using Vkontakte;
using System.Collections.Generic;

public class FriendToInvite : MonoBehaviour
{
    public tk2dTextMesh Name;
    public Avatar avatar;

    public VkUser Friend { get { return friend; } }

    [SerializeField] 
    private tk2dUILayout layout;
    [SerializeField]
    private Renderer bgRenderer;
    private VkUser friend;

    public void Init(VkUser vkuser)
    {
        avatar.Init(new Player(SocialPlatform.Vkontakte, vkuser.Id));
        avatar.DownloadAvatar();
        friend = vkuser;
        Name.text = string.Format("{0} {1}", friend.FirstName, friend.LastName);
    }

    public float ItemHeight
    {
        get
        {
            if(layout)
                return (layout.GetMaxBounds() - layout.GetMinBounds()).y;
            else
                return bgRenderer.bounds.size.y;
        }
    }

    IEnumerator Invite()
    {
        var parameters = new Dictionary<string,string>{
            {"user_id", friend.Id},
            {"text", WWW.EscapeURL(Localizer.GetText("textVkInvitation"))},
            {"type", "invite"}
        };
        yield return StartCoroutine(VkApi.apps().sendRequest(parameters).Start(delegate(string jsonResponse) {
            //Debug.Log("sendRequest "+jsonResponse);
            var prefs = new JsonPrefs(jsonResponse);
            if (prefs.Contains("error"))
            {
                var error_code = prefs.ValueInt("error/error_code");
                switch (error_code)
                {
                    case 9://flood control
                        //MessageBox ? превышено количество сообщений, больше сегодня друзей пригласить нельзя
                        MessageBox.Show(MessageBox.Type.Info, Localizer.GetText("vkFloodControl"));
                        break;
                    case 15:
                        MessageBox.Show(MessageBox.Type.Info, Localizer.GetText("vkInviteDisallowed"));
                        break;
                    case 17:
                        Application.OpenURL(prefs.ValueString("error/redirect_uri"));
                        GetFuelForInvite();
                        break;
                }
            }
            else
            { 
                GetFuelForInvite();
            }
        }));        
    }

    private void GetFuelForInvite()
    {
        if (SocialSettings.IsBonusForInviteAvailable)
        {
            Http.Manager.FuelForInvite((b, response) =>
            {
                if (b)
                {
                    #region Google Analytics: fuel got via social invitation

                    GoogleAnalyticsWrapper.LogEvent(
                        new CustomEventHitBuilder()
                            .SetParameter(GAEvent.Category.FuelBuying)
                            .SetParameter(GAEvent.Action.GotViaMoimirInvitation)
                            .SetParameter<GAEvent.Label>()
                            .SetSubject(GAEvent.Subject.PlayerLevel, ProfileInfo.Level)
                            .SetValue(ProfileInfo.Gold));

                    #endregion
                }
            });
        }
    }
}
