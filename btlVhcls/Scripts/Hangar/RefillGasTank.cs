using System;
using System.Globalization;
using UnityEngine;

public class RefillGasTank : MonoBehaviour
{

    public tk2dTextMesh timer;
    public tk2dTextMesh inviteBonusValue;
    public HorizontalLayout inviteButtonLayout;
    public GameObject inviteBonus;
    [SerializeField]private tk2dTextMesh[] lblRefillAllGas;

    private static int minutes;
    private static int seconds;
    private static int refillSecondsRemainingCell;

    public static RefillGasTank instance;

    public static int RefillSecondsRemainingFull { get; private set; }

    void Awake()
    {
        instance = this;
    }

    void OnDestroy()
    {
        instance = null;
    }

    void OnEnable () {
		SocialSettings.OnLastInviteChanged += LastFriendInviteChanged;
    	inviteBonusValue.text = "+" + GameData.fuelForInvite;
        Dispatcher.Subscribe (EventId.FriendInviteSuccess, FriendInviteSuccess);
        HangarController.OnTimerTick += UpdateFuel;
    }

    void OnDisable () {
		SocialSettings.OnLastInviteChanged -= LastFriendInviteChanged;
        Dispatcher.Unsubscribe (EventId.FriendInviteSuccess, FriendInviteSuccess);
        HangarController.OnTimerTick -= UpdateFuel;
    }

    void FriendInviteSuccess (EventId id, EventInfo info)
    {
        if (GUIPager.ActivePageName != "RefillGasTank") {
            return;
        }

        Cancel();
    }

    void LastFriendInviteChanged () {
        inviteBonus.SetActive(SocialSettings.IsBonusForInviteAvailable);

		if(inviteButtonLayout != null)
    		inviteButtonLayout.Align ();
    }

    void UpdateFuel(double time)
    {
        if (ProfileInfo.Fuel < ProfileInfo.MaxFuel)
        {
            refillSecondsRemainingCell = Convert.ToInt32(GameData.refuellingTime * (1 - (ProfileInfo.Fuel - (int)ProfileInfo.Fuel)));
            timer.text = GetTimeForTimer(refillSecondsRemainingCell);
            RefillSecondsRemainingFull = Convert.ToInt32((ProfileInfo.MaxFuel - ProfileInfo.Fuel) * GameData.refuellingTime);
        }
        else
        {
            timer.text = GetTimeForTimer(0);
        }
    }

    public void ShowRefillGasTankWindow()
    {
#if !UNITY_EDITOR
        if (ProfileInfo.Fuel < ProfileInfo.MaxFuel)
        {
#endif
        GUIPager.SetActivePage("RefillGasTank", true);
        HelpTools.SetTextToAllLabelsInCollection(lblRefillAllGas, Localizer.GetText("lblRefillGasTank", 3));
        LastFriendInviteChanged ();
#if !UNITY_EDITOR
        }
#endif
    }

    public void InviteFriends () {
		SocialSettings.GetSocialService().InviteFriend();
    }

    private string GetTimeForTimer(int time)
    {
        minutes = time / 60;
        seconds = time - minutes * 60;

        return String.Format("{0:0}:{1:00}", minutes, seconds);

    }


    bool m_isBusy = false;
    private void BuyFuel()
    {
        if (ProfileInfo.Gold >= ProfileInfo.GoldForFullFuelTank && ProfileInfo.Fuel < ProfileInfo.MaxFuel)
        {
            if (m_isBusy)
            {
                return;
            }
            m_isBusy = true;
            var request = Http.Manager.Instance().CreateRequest("/shop/buyFullFuel");

            Http.Manager.StartAsyncRequest(
                request: request,
                successCallback: delegate (Http.Response result)
                {
                    m_isBusy = false;
                    GUIPager.Back();

                    #region Google Analytics: fuel bought for gold

                    GoogleAnalyticsWrapper.LogEvent(
                        new CustomEventHitBuilder()
                            .SetParameter(GAEvent.Category.FuelBuying)
                            .SetParameter(GAEvent.Action.Bought)
                            .SetParameter<GAEvent.Label>()
                            .SetSubject(GAEvent.Subject.PlayerLevel, ProfileInfo.Level)
                            .SetValue(ProfileInfo.Gold));

                    #endregion
                },
                failCallback: delegate (Http.Response result)
                {
                    m_isBusy = false;
                });
        }
		else if (ProfileInfo.Gold < ProfileInfo.GoldForFullFuelTank)
        {
            HangarController.Instance.GoToBank(Bank.Tab.Gold, voiceRequired: true);
        }
    }

    private void Cancel()
    {
        GUIPager.Back();

        #region Google Analytics: fuel buying canceled

        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(GAEvent.Category.FuelBuying)
                .SetParameter(GAEvent.Action.ClosedWindow)
                .SetParameter<GAEvent.Label>()
                .SetSubject(GAEvent.Subject.PlayerLevel, ProfileInfo.Level)
                .SetValue(ProfileInfo.Gold));

        #endregion
    }
}
