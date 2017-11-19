using System.Linq;
using UnityEngine;

public class ConsumableKitOffer : SpecialOffer
{
    protected int discount;
    public int Discount { get { return discount; } }

    public override bool IsLimited
    {
        get { return false;}
        protected set { }
    }

    override protected void Awake()
    {
        base.Awake ();
        Dispatcher.Subscribe(EventId.OnLanguageChange, SetInfo);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy ();
        Dispatcher.Unsubscribe(EventId.OnLanguageChange, SetInfo);
    }

    protected override void OnTick(double tick)
    {
        base.OnTick(tick);

        if (Remain <= 0)
        {
            HangarController.OnTimerTick -= OnTick;
            UpdateItem();
        }
    }

    public void Initialize(int id, int discount, double endTime)
    {
        this.id = id;
        this.discount = discount;
        this.endTime = endTime;

        InitializeOfferFrame();
    }

    public override void UpdateItem()
    {
        base.UpdateItem();

        SetPrice();

        if (Remain > 0)
            offerFrame.Show();
        else
            ResetItem();
    }

    protected override void SetBtn()
    {
        base.SetBtn();
        offerFrame.btnBuy.OnClickUIItem += SpecialOffersPage.ConsumableKitOffersBtnClickHandler;
    }

    protected override void SetInfo(EventId eventId = 0, EventInfo info = null)
    {
        offerFrame.info.text = Localizer.GetText(GameData.consumableKitInfos[id].localizationKey);
    }

    protected override void SetPrice()
    {
        ProfileInfo.Price p = GameData.consumableKitInfos[id].price;
        offerFrame.oldPriceRenderer.OldPrice = p;
        offerFrame.priceRenderer.Price = new ProfileInfo.Price((int)(p.value * (1 - discount * 0.01f)), p.currency);
    }

    protected override void SetSprite()
    {
        offerFrame.sprProduct.SetSprite(GameData.consumableKitInfos[id].icon);
    }

    protected virtual void ResetItem()
    {
        HangarController.OnTimerTick -= OnTick;
    }
}
