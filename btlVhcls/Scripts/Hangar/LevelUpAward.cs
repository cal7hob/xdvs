using System;
using GAEvent;
using Http;
using UnityEngine;
using Action = GAEvent.Action;

public class LevelUpAward : MonoBehaviour, IQueueablePage
{
    public enum Award
    {
        GoldKit, //инап xdevs.gold_kit
        Gold,
        Silver,
        Fuel,
    }

    [SerializeField] private tk2dSprite sprCurrency;
	[SerializeField] private tk2dTextMesh levelUpAwardValue;
    [SerializeField] private tk2dTextMesh lblHeader;
    [SerializeField] private tk2dSprite sprFuel;
	[SerializeField] private tk2dUIItem okBtn;
    [SerializeField] private Award currentAward;

    private int silverAward;
    private int goldAward;

    private bool isSaving;
    private bool showGoldKitAward;

    private void Awake()
    {
        Dispatcher.Subscribe(EventId.AfterHangarInit, CheckForLevelUpPopup);
    }

    private void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.AfterHangarInit, CheckForLevelUpPopup);
    }

    public void CheckForLevelUpPopup(EventId id, EventInfo info)
    {
        if (ProfileInfo.Level > ProfileInfo.lastLevelUpAward
            || !ProfileInfo.goldKitAwardIsObtained)
        {
            if (GUIPager.ActivePageName != "LevelUpAward")
                GUIPager.EnqueuePage("LevelUpAward", false);
            else
                BeforeActivation();

            return;
        }
        
        if (GUIPager.ActivePageName == "LevelUpAward")
            GUIPager.ToMainMenu();
    }

    public void BeforeActivation()
    {
        if (!ProfileInfo.goldKitAwardIsObtained) //если надо показать окно награды за vip_kit
        {
            Setup(Award.GoldKit);
            return;
        }

        if (ProfileInfo.Level > ProfileInfo.lastLevelUpAward)
        {
            var level = ProfileInfo.lastLevelUpAward + 1;
            silverAward = level * GameData.levelUpAwardCoefficientSilver;
            goldAward = (int)Math.Ceiling(level * GameData.levelUpAwardCoefficientGold);

            Setup(Award.Gold);

            if (SocialSettings.IsWebPlatform)
                SocialSettings.GetSocialService().PostNewLevelToWall();

            #region Google Analytics: leveling up

            GoogleAnalyticsWrapper.LogEvent(
                new CustomEventHitBuilder()
                    .SetParameter(Category.LevelUp)
                    .SetParameter<Action>()
                    .SetSubject(Subject.PlayerLevel, ProfileInfo.Level)
                    .SetParameter<Label>()
                    .SetSubject(Subject.VehicleID, ProfileInfo.currentVehicle));

            GoogleAnalyticsWrapper.LogEvent(
                new CustomEventHitBuilder()
                    .SetParameter(Category.LevelUp)
                    .SetParameter<Action>()
                    .SetSubject(Subject.PlayerLevel, ProfileInfo.Level)
                    .SetParameter<Label>()
                    .SetSubject(Subject.GoldAmount, ProfileInfo.Gold));

            GoogleAnalyticsWrapper.LogEvent(
                new CustomEventHitBuilder()
                    .SetParameter(Category.LevelUp)
                    .SetParameter<Action>()
                    .SetSubject(Subject.PlayerLevel, ProfileInfo.Level)
                    .SetParameter<Label>()
                    .SetSubject(Subject.SilverAmount, ProfileInfo.Silver));

            #endregion
        }
    }

    public void Activated() { }

    private void Setup(Award award)
    {
        currentAward = award;

        sprFuel.gameObject.SetActive(false);
        sprCurrency.gameObject.SetActive(false);
        lblHeader.GetComponent<LabelLocalizationAgent>().key = "lblLevelUp";

        switch (award)
        {
            case Award.Gold:
                levelUpAwardValue.text = MiscTools.GetCultureSpecificFormatOfNumber(goldAward);
                sprCurrency.SetSprite(GameData.IsGame(Game.IronTanks) ? "bonus_gold" : "gold_2");
                sprCurrency.gameObject.SetActive(true);
                break;
            case Award.Silver:
                levelUpAwardValue.text = MiscTools.GetCultureSpecificFormatOfNumber(silverAward);
                sprCurrency.SetSprite(GameData.IsGame(Game.IronTanks) ? "bonus_silver" : "silver_2");
                sprCurrency.gameObject.SetActive(true);
                break;
            case Award.Fuel:
                sprFuel.gameObject.SetActive(true);
                levelUpAwardValue.text = Localizer.GetText("lblFullTank");
                break;
            case Award.GoldKit:
                sprCurrency.gameObject.SetActive(true);
                sprCurrency.SetSprite(GameData.IsGame(Game.IronTanks) ? "bonus_gold" : "gold_2");
                levelUpAwardValue.text = MiscTools.GetCultureSpecificFormatOfNumber(ProfileInfo.goldKitAwardVal);
                lblHeader.GetComponent<LabelLocalizationAgent>().key = "lblGoldKitAwardHeader";
                break;
        }

        lblHeader.GetComponent<LabelLocalizationAgent>().LocalizeLabel();
    }

    #region Обработка кнопок окна

    private void OnButtonClick(tk2dUIItem btn)
    {
        //Debug.LogError("OnButtonClick: currentAward == " + currentAward);

        switch (currentAward)
        {
            case Award.Gold: Setup(Award.Silver); break;
            case Award.Silver: Setup(Award.Fuel); break;
            case Award.Fuel:
                if (isSaving)
                    break;

                RequestAwardFromServer((resp, result) =>
                {
                    //Зацикливание - показываем окно повышения уровня пока не получим награды за все полученные уровни
                    if (result)
                    {
                        HangarController.Instance.PlaySound(HangarController.Instance.buyingSound);
                        CheckForLevelUpPopup(EventId.Manual, null);
                    }
                });

                break;
            case Award.GoldKit:
                RequestAwardForGoldKitFromServer(
                    () =>
                    {
                        HangarController.Instance.PlaySound(HangarController.Instance.buyingSound);
                        ProfileInfo.goldKitAwardIsObtained = true;
                        CheckForLevelUpPopup(EventId.Manual, null);
                    });

                break;
        }
    }

    private void RequestAwardFromServer(Action<Response, bool> finishCallback)
    {
        isSaving = true;

        var request = Manager.Instance().CreateRequest("/player/getLevelUpAward");
        Manager.StartAsyncRequest(request,
            delegate (Response result)
            {
                isSaving = false;
                finishCallback(result, true);
            },
            delegate (Response result)
            {
                isSaving = false;
                finishCallback(result, false);
            }
        );
    }

    private void RequestAwardForGoldKitFromServer(System.Action finishCallback)
    {
        var request = Manager.Instance().CreateRequest("/player/getNewbieGoldKitReward");
        Manager.StartAsyncRequest(request, success => { finishCallback(); }, fail => { finishCallback(); });
    }

    #endregion
}
