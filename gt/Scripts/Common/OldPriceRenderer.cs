using UnityEngine;
using System.Collections;

public class OldPriceRenderer : MonoBehaviour
{
    public tk2dTextMesh oldPriceAmount;
    public tk2dSlicedSprite sprRedLine;
    public tk2dSprite oldCurrencyCoins;
    public GameObject wrapper;
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
            oldPrice.SetMoneySpecificColorIfCan(oldPriceAmount);
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
        oldCurrencyCoins.SetSprite(spriteName);
    }

    private void OnLanguageChange(EventId id, EventInfo info)
    {
        StretchCrossingLine(oldPriceAmount.text);
    }
}
