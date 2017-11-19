using UnityEngine;
using System.Collections;

public class VipOfferPrefab : MonoBehaviour, IVipOfferPrefab
{

    public tk2dTextMesh expirationLable;
    public tk2dTextMesh localizedPrice;
    public GameObject buyButton;
    public tk2dSlicedSprite offerBackground;
    public OldPriceRenderer oldPriceRenderer;
    public HorizontalLayout durationAligner;

    public float OfferWidth { get { return offerBackground.dimensions.x * offerBackground.scale.x + Margin; } }

    public int OfferId { get; set; }

    public string OfferUnibillerId { get; set; }

    public int ShopPosition { get; set; }

    public int VipDurationDays { get; set; }

    public float Margin
    {
        get { return _margin; }
        set { _margin = value; }
    }
    private float _margin = 100;


    public void UpdateOfferInstance()
    {
        gameObject.transform.localPosition = new Vector3(
			(offerBackground.dimensions.x * offerBackground.scale.x + Margin) * ShopPosition + offerBackground.dimensions.x * offerBackground.scale.x / 2f,
            0,
            0);
    }

    private void OnClick()
    {
        IapManager.BuyProductId(offerBackground.transform, OfferUnibillerId);
    }
}
