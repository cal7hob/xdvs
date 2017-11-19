using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Http;

public class BonusFrame : MonoBehaviour {

    public tk2dUIItem takeBonusBtn;
	public ActivatedUpDownButton objectsToChangeAlphaWhenDayPassed;
    public ActivatedUpDownButton objectsToActivateForCurrentDay;//Объекты, активируемые на плашке текущего дня
    public ActivatedUpDownButton objectsToActivateInFutureDays;//Объекты, активируемые на плашках последующих дней(после текущего дня)
    public GameObject sprArrow;
    public tk2dSprite sprCurrencyBig;
    public tk2dTextMesh[] dayNumLabels;
	public tk2dTextMesh awardText;
	public tk2dSprite awardSprite;
    private bool btnDisabled = false;//Чтобы не обрабатывать клики пока ждем ответа от сервера


    public void OnClickObtainDailyBonus()
    {
        if (btnDisabled)
            return;

        try
        {
            btnDisabled = true;
            var request = Http.Manager.Instance().CreateRequest("/player/getDailyBonus");
            request.Form.AddField("playerId", ProfileInfo.profileId);
            Http.Manager.StartAsyncRequest(request,
                successCallback: OnDailyBonusObtained,
                failCallback: (Response result) =>
                {
                    btnDisabled = false;
                    Debug.LogError("Failed to obtain daily bonus");
                });
        }
        catch (System.Exception e)
        {
            Http.Manager.ReportException("DailyBonus.TakeDailyBonusBtn", e);
            btnDisabled = false;
        }
    }

    private void OnDailyBonusObtained(Response result)
    {
        btnDisabled = false;
        ProfileInfo.dailyBonusIsObtained = true;
        HangarController.Instance.ShowUserInfo();
        GUIPager.ToMainMenu();
    }

    public void Setup(KeyValuePair<int, ProfileInfo.Price> bonusData, bool isLast)
    {
        if (sprArrow) sprArrow.SetActive(!isLast);
        if (awardText)
        {
            awardText.text = bonusData.Value.LocalizedValue;
            if (awardText.inlineStyling)
                bonusData.Value.SetMoneySpecificColorIfCan (awardText);
        }
        if (awardSprite) awardSprite.SetSprite(bonusData.Value.currency == ProfileInfo.PriceCurrency.Silver ? "silver" : "gold");
        if (dayNumLabels != null)
            for (int j = 0; j < dayNumLabels.Length; j++)
                if (dayNumLabels[j] != null)
                    dayNumLabels[j].text = Localizer.GetText("lblBonusDay", bonusData.Key);

        if (bonusData.Value.currency == ProfileInfo.PriceCurrency.Gold)
        {
            SetSpriteDependingOnBonusAmount(0, 5, bonusData.Value.value, "gold_1");
            SetSpriteDependingOnBonusAmount(6, 10, bonusData.Value.value, "gold_2");
            SetSpriteDependingOnBonusAmount(11, 15, bonusData.Value.value, "gold_3");
            SetSpriteDependingOnBonusAmount(16, 20, bonusData.Value.value, "gold_4");
            SetSpriteDependingOnBonusAmount(21, 25, bonusData.Value.value, "gold_5");
            SetSpriteDependingOnBonusAmount(26, 100000, bonusData.Value.value, "gold_5");
        }
        else
        {
            SetSpriteDependingOnBonusAmount(0, 1000, bonusData.Value.value, "silver_1");
            SetSpriteDependingOnBonusAmount(1001, 2000, bonusData.Value.value, "silver_2");
            SetSpriteDependingOnBonusAmount(2001, 3000, bonusData.Value.value, "silver_3");
            SetSpriteDependingOnBonusAmount(3001, 4000, bonusData.Value.value, "silver_4");
            SetSpriteDependingOnBonusAmount(4001, 5000, bonusData.Value.value, "silver_5");
            SetSpriteDependingOnBonusAmount(5001, 5000000, bonusData.Value.value, "silver_5");
        }

        if (objectsToChangeAlphaWhenDayPassed)
            objectsToChangeAlphaWhenDayPassed.Activated = false;//Выключаем объекты, активируемые для прошедших дней по умолчанию
        if (objectsToActivateInFutureDays)
            objectsToActivateInFutureDays.Activated = false;//Выключаем объекты, активируемые для будущих дней по умолчанию
        if (objectsToActivateForCurrentDay)
            objectsToActivateForCurrentDay.Activated = false;//Выключаем объекты, активируемые для текущего дня по умолчанию

        if (bonusData.Key - 1 == ProfileInfo.dailyBonusIndex)//Текущий день
        {
            takeBonusBtn.OnClick -= OnClickObtainDailyBonus;//Защита от повторной подписки
            takeBonusBtn.OnClick += OnClickObtainDailyBonus;
            if (objectsToActivateForCurrentDay)
                objectsToActivateForCurrentDay.Activated = true;
        }
        else if (bonusData.Key - 1 < ProfileInfo.dailyBonusIndex)//Раньше текущего
        {
            if (objectsToChangeAlphaWhenDayPassed)
                objectsToChangeAlphaWhenDayPassed.Activated = true;
        }
        else//После текущего
        {
            if (objectsToActivateInFutureDays)
                objectsToActivateInFutureDays.Activated = true;
        }

    }

    private void SetSpriteDependingOnBonusAmount(int min, int max, int amount, string spriteName)
    {
        if (min <= amount && amount <= max)
            sprCurrencyBig.SetSprite(spriteName);
    }
}
