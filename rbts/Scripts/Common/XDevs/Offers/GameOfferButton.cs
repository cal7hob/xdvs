using UnityEngine;
using System;
using System.Collections;

namespace XDevs.Offers
{
    public class GameOfferButton : MonoBehaviour
    {
        [SerializeField]
        public GameObject wrapper;
        [SerializeField]
        tk2dBaseSprite iconUp;
        [SerializeField]
        tk2dBaseSprite iconDown;

        [SerializeField]
        tk2dTextMesh lblGameName;
        [SerializeField]
        Color gameNameTextColor = Color.white;
        [SerializeField]
        Color gameNameColor = new Color(1, 0.7f, 0, 1);

        [SerializeField]
        ButtonsPanel.PanelButton rewardLine;
        [SerializeField]
        tk2dTextMesh lblReward;
        [SerializeField]
        Color awardTextColor = Color.white;
        [SerializeField]
        MoneyIcon moneyIcon;

        public event Action<GameOfferButton> Clicked = delegate (GameOfferButton btn) { };

        public GameOffer gameOffer;

        public void SetGame (GameOffer offer)
        {
            try {
                gameOffer = offer;

                iconUp.SetSprite(offer.id);
                if(iconDown)
                    iconDown.SetSprite(offer.id);

                UpdateElements();
            }
            catch (Exception e) {
                Debug.LogException(e);
            }
        }

        private void UpdateElements()
        {
            lblGameName.text = Localizer.GetText("lblXDevsGameOfferName",
                gameNameTextColor.To2DToolKitColorFormatString(),       //цвет слова Установи
                gameNameColor.To2DToolKitColorFormatString(),           //цвет игры
                gameOffer.name);                                        //название игры

            if (gameOffer.award.value > 0)
            {
                lblReward.text = Localizer.GetText("lblXDevsGameOfferReward",
                    lblReward.inlineStyling ? awardTextColor.To2DToolKitColorFormatString() : "",
                    lblReward.inlineStyling ? gameOffer.award.MoneySpecificColor.To2DToolKitColorFormatString() : "",
                    gameOffer.award.value.ToString("N0", GameData.instance.cultureInfo.NumberFormat));

                moneyIcon.SetCurrency(gameOffer.award.currency);
            }
            else {
                rewardLine.gameObject.SetActive(false);
            }
        }
             


        void Awake()
        {
            Messenger.Subscribe(EventId.OnLanguageChange, OnLanguageChange);
        }

        void OnDestroy()
        {
            Messenger.Unsubscribe(EventId.OnLanguageChange, OnLanguageChange);
        }

        private void OnLanguageChange(EventId evId, EventInfo ev)
        {
            UpdateElements();
        }

        void OnClicked ()
        {
            Clicked(this);
        }

    }
}
