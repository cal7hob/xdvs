using System;
using UnityEngine;

public class MoneyIcon : MonoBehaviour
{
    [Serializable]
    public class CurrencySpriteMatch
    {
        public ProfileInfo.PriceCurrency name;
        public string spriteNameForCurrency;
    }

    [SerializeField] private CurrencySpriteMatch[] currencySpriteMatches;
    [SerializeField] private tk2dBaseSprite sprMoney;

    [SerializeField] private ProfileInfo.PriceCurrency currency;
    public ProfileInfo.PriceCurrency Currency { get { return currency; } }

    public void SetCurrency(ProfileInfo.PriceCurrency currency)
    {
        this.currency = currency;
        bool spriteMatched = false;

        foreach (var currencySpriteMatch in currencySpriteMatches)
        {
            if (currencySpriteMatch.name == currency)
            {
                sprMoney.SetSprite(currencySpriteMatch.spriteNameForCurrency);
                spriteMatched = true;
                break;
            }
        }

        if (!spriteMatched)
        {
            Debug.LogErrorFormat(this, "No sprite match for currency {0} found", currency);
        }
    }
}