using UnityEngine;
using System.Collections;

public class OldPriceRenderer : MonoBehaviour
{
    public GameObject wrapper;
    public tk2dTextMesh oldPriceAmount;
    public tk2dSlicedSprite sprRedLine;
    public tk2dSprite oldCurrencyCoins;
    public bool colorizeCrossedLineAsMoneyLabel = false;

    private ProfileInfo.Price oldPrice;

    public string OldPriceAmount 
    {
        get { return oldPriceAmount.text; }
        set
        {
            oldPriceAmount.text = value;
            StretchCrossingLine(value);
        }
    }

    public ProfileInfo.Price OldPrice
    {
        set
        {
            oldPrice = value;
            StretchCrossingLine(value.LocalizedValue);
            oldPriceAmount.text = oldPrice.LocalizedValue;
            oldPriceAmount.SetMoneySpecificColorIfCan(oldPrice);
            if (sprRedLine && colorizeCrossedLineAsMoneyLabel)
                sprRedLine.color = new Color(oldPrice.MoneySpecificColor.r, oldPrice.MoneySpecificColor.g, oldPrice.MoneySpecificColor.b, sprRedLine.color.a);

            if (oldCurrencyCoins)
                SetCurrencySprite();
        }
        get { return oldPrice; }
    }

    void Awake()
    {
        Dispatcher.Subscribe(EventId.OnLanguageChange, OnLanguageChange);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.OnLanguageChange, OnLanguageChange);
    }

    public void SetActive(bool activate)
    {
        if (activate)
        {
            if (!wrapper.activeSelf)
                wrapper.SetActive(true);
        }
        else
        {
            if (wrapper.activeSelf)
                wrapper.SetActive(false);
        }
    }

    private void StretchCrossingLine(string str)
    {
        if (sprRedLine == null)
            return;
        var sprDimensions = sprRedLine.dimensions;
        sprDimensions.x = oldPriceAmount.GetEstimatedMeshBoundsForString(str).size.x + 40;
        sprRedLine.dimensions = sprDimensions;
    }

    private void SetCurrencySprite()
    {
        var spriteName = oldPrice.currency.ToString().ToLowerInvariant();
        if (GameData.IsGame(Game.FutureTanks))
            spriteName += "_b";

        oldCurrencyCoins.SetSprite(spriteName);
    }

    private void OnLanguageChange(EventId id, EventInfo info)
    {
        StretchCrossingLine(oldPriceAmount.text);
    }
}
