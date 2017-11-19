using UnityEngine;
using System.Collections;

public class PriceRenderer : MonoBehaviour
{
    [SerializeField] protected tk2dBaseSprite sprCoins;
    [SerializeField] protected tk2dTextMesh amount;
    [SerializeField] private ProfileInfo.Price price;
    
    public ProfileInfo.Price Price
    {
        get { return price; }
        set
        {
            price = value;
            UpdateElements();
        }
    }

    public tk2dTextMesh Amount { get { return amount; } }//TODO: Удалить все случаи использования. Быдлокод. Можно менять текст только присваивая price

    protected virtual void UpdateElements()
    {
        var spriteName = price.currency.ToString().ToLowerInvariant();
        if (GameData.IsGame(Game.FutureTanks))
            spriteName += "_b";

        sprCoins.SetSprite(spriteName);

        amount.text = price.LocalizedValue;
        amount.SetMoneySpecificColorIfCan(price);
    }
}
