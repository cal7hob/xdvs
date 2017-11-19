using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class SpecialOffersController<TOffer> : MonoBehaviour where TOffer : SpecialOffer
{
    protected bool initialized;

    /// <summary>
    /// общая для всех офферов одного вида наклейка SALE 
    /// </summary>
    public SaleSticker saleSticker; 

    /// <summary>
    /// Словарь офферов одного вида, где ключ == Id оффера
    /// </summary>
    public abstract Dictionary<int, TOffer> Offers { get; }

    /// <summary>
    /// Есть ли хоть один оффер одного вида на акции
    /// </summary>
    public virtual bool AnyItemIsOnSale
    {
        get
        {
            return Offers != null && Offers.Any(offer => offer.Value.Remain > 0);
        }
    }

    protected virtual int MaxDiscount
    {
        get
        {
            int maxDiscount = -1;
            foreach (var offer in Offers.Values)
            {
                var shopOffer = offer as ShopOffer;

                if (shopOffer == null || shopOffer.Remain < 0 || shopOffer.IsOwned) continue;

                if (maxDiscount < shopOffer.Discount)
                {
                    maxDiscount = shopOffer.Discount;
                }
            }

            return maxDiscount;
        }
    }

    /// <summary>
    /// инициализация контроллера
    /// </summary>
    public virtual void Init(EventId id, EventInfo info)
    {
        ParseOffers();
        SubscribeOnTickSaleSticker();
        SetSpecialOffersList();

        if (VipOffersController.Instance.initialized && 
            VehicleOffersController.Instance.initialized &&
            DecalOffersController.Instance.initialized &&
            PatternOffersController.Instance.initialized &&
            BankOffersController.Instance.initialized)
        {
            Messenger.Send(EventId.SpecialOffersInitialized, new EventInfo_SimpleEvent());
        }
    }

    protected abstract void SetSpecialOffersList();

    /// <summary>
    /// подписка на ежесекундную проверку SaleSticker`а самого магазина, чтобы выключить, когда кончатся офферы
    /// </summary>
    public void SubscribeOnTickSaleSticker()
    {
        HangarController.OnTimerTick += SetHangarShopSaleSticker;
    }

    /// <summary>
    /// разбор пришедшей с сервера инфы по офферам и создание словаря офферов этого вида
    /// </summary>
    protected abstract void ParseOffers();

    public virtual void RefreshOffers()
    {
        HangarController.OnTimerTick -= SetHangarShopSaleSticker;
        HangarController.OnTimerTick += SetHangarShopSaleSticker;

        foreach (var offer in Offers)
        {
            offer.Value.UnsubscribeFromTimer();
            offer.Value.SubscribeOnTimer();
            offer.Value.UpdateItem();
        }
    }

    protected virtual void Awake()
    {
        Messenger.Subscribe(EventId.AfterHangarInit, Init);
    }

    protected virtual void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.AfterHangarInit, Init);
    }

    /// <summary>
    /// вкл/выкл стикера SALE на кнопках входа в окна магазинов ангара
    /// </summary>
    protected void SetHangarShopSaleSticker(double d)
    {
        if (!saleSticker) return;

        if (!SetOffersSaleSticker())
            HangarController.OnTimerTick -= SetHangarShopSaleSticker;
    }

    /// <summary>
    /// установка SaleSticker`а над магазином
    /// </summary>
    /// <returns></returns>
    protected bool SetOffersSaleSticker()
    {
        if (AnyItemIsOnSale)
        {
            saleSticker.SetActive(true);
            saleSticker.Text = SpecialOffersPage.Instance.GetSaleStickerText(MaxDiscount);
            return true;
        }

        saleSticker.SetActive(false);
        return false;
    }

    protected abstract bool IsOfferedItemIsLocked(int id);

    public abstract bool CheckIfItemOnSale(int itemId);
}
