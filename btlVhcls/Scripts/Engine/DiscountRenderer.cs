using UnityEngine;

public class DiscountRenderer : MonoBehaviour
{
    [SerializeField] private PriceRenderer curPrice;
    [SerializeField] private PriceRenderer oldPrice;
    [SerializeField] private tk2dSlicedSprite crossedLine;
    public bool colorizeCrossedLineAsMoneyLabel = false;

    public void SetCurPrice(ProfileInfo.Price p)
    {
        if (!p)
            return;
        curPrice.Price = p;
    }

    public void SetOldPrice(ProfileInfo.Price p, bool isOldPriceActive)
    {
        oldPrice.gameObject.SetActive(p != null && isOldPriceActive);
        if (!oldPrice.gameObject.activeSelf)
            return;

        oldPrice.Price = p;
        if(colorizeCrossedLineAsMoneyLabel && crossedLine)
            crossedLine.color = new Color(p.MoneySpecificColor.r, p.MoneySpecificColor.g, p.MoneySpecificColor.b, crossedLine.color.a);
    }

    public void SetupPrices(ProfileInfo.Price curP, ProfileInfo.Price oldP, bool isOldPriceActive)
    {
        SetCurPrice(curP);
        SetOldPrice(oldP, isOldPriceActive);
    }
}
