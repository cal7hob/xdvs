
public class BankOffer : SpecialOffer
{
    protected ProfileInfo.Price price;
    protected string xDevsKey;

    public ProfileInfo.Price Price { get { return price; } }
    public string XDevsKey { get { return xDevsKey; } }

    public override bool IsLimited {
        get {
            return (
                ((price.currency == ProfileInfo.PriceCurrency.Gold) && ProfileInfo.IsGoldDiscountLimitReached)
                ||
                ((price.currency == ProfileInfo.PriceCurrency.Silver) && ProfileInfo.IsSilverDiscountLimitReached)
            );
        }

        protected set {}
    }

    public void Initialize(string xDevsKey, ProfileInfo.Price price, double endTime)
    {
        this.xDevsKey = xDevsKey;
        this.price = price;
        this.endTime = endTime;

        InitializeOfferFrame();
    }

    public override void UpdateItem()
    {
        base.UpdateItem();
        BankData.prices[XDevsKey].bankLot.Init();
    }

    protected override void SetInfo(EventId eventId = 0, EventInfo info = null)
    {    
    }

    protected override void SetPrice()
    {        
        offerFrame.priceRenderer.Price = price;
        offerFrame.oldPriceRenderer.OldPrice = BankData.prices[XDevsKey].FullPrice;
        offerFrame.lblBuy.gameObject.SetActive(true);
    }

    protected override void SetBtn()
    {
        offerFrame.btnBuy.OnClickUIItem += SpecialOffersPage.BankOffersBtnClickHandler;
        offerFrame.lblBuy.text = PriceLocalizationAgent.GetLocalizedString(XDevsKey, offerFrame.lblBuy, BankData.prices[XDevsKey].LocalizationKey);
    }

    protected override void SetSprite()
    {
        offerFrame.sprProduct.SetSprite(BankData.prices[XDevsKey].SpriteName);
    }
}
