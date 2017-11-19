using UnityEngine;
using System.Collections;

public class VipOfferPrefab : MonoBehaviour
{
    /// <summary>
    /// button to buy vip account
    /// </summary>
    public GameObject buyButton;

    /// <summary>
    /// капитан говорит, что это для показа старой цены
    /// </summary>
    public OldPriceRenderer oldPriceRenderer;

    /// <summary>
    /// для выравнивания лейбла длительности випа, когда истекает акция
    /// </summary>
    public HorizontalLayout durationAligner;


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
       
    }

    private void OnClick()
    {
        /*if (VipManager.Instance.VipShopScrollableArea.IsSwipeScrollingInProgress)
            return;*/
    }
}
