using UnityEngine;
using System.Collections;

public class OldPriceRenderer : MonoBehaviour
{
    public GameObject wrapper;
    public bool colorizeCrossedLineAsMoneyLabel = false;

    private ProfileInfo.Price oldPrice;

    public ProfileInfo.Price OldPrice
    {
        set
        {
            oldPrice = value;
            StretchCrossingLine(value.LocalizedValue);
           
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
        
    }

    private void SetCurrencySprite()
    {
        var spriteName = oldPrice.currency.ToString().ToLowerInvariant();
        if (GameData.IsGame(Game.FutureTanks))
            spriteName += "_b";
    }

    private void OnLanguageChange(EventId id, EventInfo info)
    {
    }
}
