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
        Color gameNameTextColor = Color.white;
        [SerializeField]
        Color gameNameColor = new Color(1, 0.7f, 0, 1);

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

                UpdateElements();
            }
            catch (Exception e) {
                Debug.LogException(e);
            }
        }

        private void UpdateElements()
        {
            if (gameOffer.award.value > 0)
            {
                moneyIcon.IsGold = gameOffer.award.currency == ProfileInfo.PriceCurrency.Gold;
            }
        }
             


        void Awake()
        {
            Dispatcher.Subscribe(EventId.AfterHangarInit, Init);
            Dispatcher.Subscribe(EventId.OnLanguageChange, OnLanguageChange);
        }

        void OnDestroy()
        {
            Dispatcher.Unsubscribe(EventId.AfterHangarInit, Init);
            Dispatcher.Unsubscribe(EventId.OnLanguageChange, OnLanguageChange);
        }

        private void OnLanguageChange(EventId evId, EventInfo ev)
        {
            UpdateElements();
        }

        void OnClicked ()
        {
            Clicked(this);
        }

        private void Init(EventId id, EventInfo info)
        {
        }

    }
}
