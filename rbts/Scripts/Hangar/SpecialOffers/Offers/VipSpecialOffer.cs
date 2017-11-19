public class VipSpecialOffer : SpecialOffer
{
    protected int diffDurationDays;
    protected VipPrice.StoreId currentStoreId;

    private VipOfferPrefab vipOfferFrame;

    public override bool IsLimited
    {
        get { return ProfileInfo.IsVipDiscountLimitReached; }

        protected set {}
    }

    public int DiffDurationDays { get { return diffDurationDays; } }

    private bool IsOfferActive
    {
        get { return !IsLimited && VipOffersController.Instance.CheckIfVipOfferOnSale(currentStoreId.Id); }
    }

    protected override void Awake()
    {
        base.Awake();
        Messenger.Subscribe(EventId.OnLanguageChange, SetInfo, 4);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Messenger.Unsubscribe(EventId.OnLanguageChange, SetInfo);
        offerFrame.btnBuy.OnClickUIItem -= SpecialOffersPage.VipOffersBtnClickHandler;
    }

    public void Initialize(VipPrice.StoreId currentStoreId, int diffDurationDays, double endTime)
    {
        this.diffDurationDays = diffDurationDays;
        this.endTime = endTime;
        this.currentStoreId = currentStoreId;
        
        vipOfferFrame = VipManager.Instance.vipOffers[currentStoreId.Id];
        VipOffersController.Instance.vipSpecialOffers.Add(currentStoreId.Id, this);

        InitializeOfferFrame();
    }

    protected override void SetInfo(EventId eventId = 0, EventInfo info = null)
    {
        if (IsOfferActive)
        {
            vipOfferFrame.oldPriceRenderer.SetActive(true);

            vipOfferFrame.oldPriceRenderer.OldPriceAmount = PriceLocalizationAgent.GetLocalizedString("lblVipOfferTime" + id, vipOfferFrame.expirationLable);
            offerFrame.oldPriceRenderer.OldPriceAmount = PriceLocalizationAgent.GetLocalizedString("lblVipOfferTime" + id, vipOfferFrame.expirationLable);

            vipOfferFrame.expirationLable.text = (VipManager.Instance.vipOffers[currentStoreId.Id].VipDurationDays + diffDurationDays).ToString();
            offerFrame.info.text = vipOfferFrame.expirationLable.text;
        }
        else
        {
            vipOfferFrame.oldPriceRenderer.SetActive(false);
            vipOfferFrame.expirationLable.text = PriceLocalizationAgent.GetLocalizedString("lblVipOfferTime" + id, vipOfferFrame.expirationLable);
            vipOfferFrame.durationAligner.Align();
        }
    }

    protected override void SetPrice()
    {
        offerFrame.priceRenderer.Amount.text = vipOfferFrame.localizedPrice.text;
    }

    public override void UpdateItem()
    {
        base.UpdateItem();
        SetInfo();
    }

    protected override void SetBtn()
    {
        base.SetBtn();
        offerFrame.btnBuy.OnClickUIItem += SpecialOffersPage.VipOffersBtnClickHandler;
    }

    protected override void SetSprite()
    {
       
    }
}
