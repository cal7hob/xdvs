using UnityEngine;

public abstract class ShopOffer : SpecialOffer
{
    protected int discount;
    protected SaleSticker saleSticker;
    protected ProfileInfo.Price discountPrice;

    public abstract bool IsOwned { get; }
    protected abstract bool IsInShop { get; }
    public int Discount { get { return discount; } }

    public ProfileInfo.Price DiscountPrice
    {
        get
        {
            return discountPrice;
        }
    }

    public virtual void Initialize(int id, int discount, double endTime)
    {
        this.id = id;
        this.discount = Mathf.Clamp(discount, 0, 100);
        this.endTime = endTime;

        InitializeOfferFrame();
    }

    protected override void InitializeOfferFrame()
    {
        SetShopItem();
        base.InitializeOfferFrame();
    }

    protected abstract void SetShopItem();
    protected abstract void UpdateSaleSticker(EventId eventId, EventInfo eventInfo);
}
