using UnityEngine;
using System.Collections;

public class IndicatorFlag : MonoBehaviour, IFlag
{

    public tk2dSlicedSprite sprFlag;
    public TankIndicator tankIndicator;

    private Vector3 playerNameDefPos;

    void Awake()
    {
        Messenger.Subscribe(EventId.FlagSettingsChanged, OnFlagSettingsChanged);
        Messenger.Subscribe(EventId.AvatarSettingsChanged, ApplyAvatarOption);

        playerNameDefPos = tankIndicator.playerName.transform.localPosition;
    }

    void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.FlagSettingsChanged, OnFlagSettingsChanged);
        Messenger.Unsubscribe(EventId.AvatarSettingsChanged, ApplyAvatarOption);
    }

    public void ApplyAvatarOption(EventId id, EventInfo info)
    {
        Vector3 namePos = playerNameDefPos;
        switch (ProfileInfo.AvatarOption)
        {
            case AvatarOption.showNothing:
                sprFlag.gameObject.SetActive(false);
                tankIndicator.avatar.Hide();
                namePos.x = sprFlag.transform.localPosition.x;
                break;

            case AvatarOption.showEverything:
                sprFlag.gameObject.SetActive(!tankIndicator.Vehicle.data.hideMyFlag);
                tankIndicator.avatar.Show();
                tankIndicator.GetAvatar();
                break;

            case AvatarOption.showOnlyFlags:
                sprFlag.gameObject.SetActive(!tankIndicator.Vehicle.data.hideMyFlag);
                tankIndicator.avatar.Hide ();
                break;

            case AvatarOption.showOnlyAvatars:
                sprFlag.gameObject.SetActive(false);
                tankIndicator.avatar.Show ();
                namePos.x = sprFlag.transform.localPosition.x;
                tankIndicator.GetAvatar();
                break;

        }
        tankIndicator.playerName.transform.localPosition = new Vector3(namePos.x, tankIndicator.playerName.transform.localPosition.y, tankIndicator.playerName.transform.localPosition.z);

        FlagSetActive(tankIndicator.Vehicle.data.hideMyFlag);
    }

    public void FlagSetActive(bool hide)
    {
        Vector3 namePos = playerNameDefPos;

        if (hide || ProfileInfo.AvatarOption == AvatarOption.showNothing)
        {
            sprFlag.gameObject.SetActive(false);
            namePos.x = sprFlag.transform.localPosition.x;
        }
        else
        {
            namePos = playerNameDefPos;
            sprFlag.gameObject.SetActive(true);
        }

        tankIndicator.playerName.transform.localPosition = new Vector3(namePos.x, tankIndicator.playerName.transform.localPosition.y, tankIndicator.playerName.transform.localPosition.z);
    }

    private void OnFlagSettingsChanged(EventId id, EventInfo info)
    {
        FlagSetActive(tankIndicator.Vehicle.data.hideMyFlag);
    }
}
