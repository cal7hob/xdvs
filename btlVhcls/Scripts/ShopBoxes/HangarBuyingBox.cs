using UnityEngine;

public class HangarBuyingBox : MonoBehaviour
{
    [SerializeField] protected ActivatedUpDownButton btnBuy;
	[SerializeField] protected tk2dTextMesh lblPrice;
	[SerializeField] protected GameObject sprSilver;
	[SerializeField] protected GameObject sprGold;
    [SerializeField] protected GameObject sprVip;

    private ProfileInfo.Price _price;
    private bool _isProductVip;

    public virtual ProfileInfo.Price Price
	{
		get { return _price; }
		set { SetPrice(new ProfileInfo.Price(value)); }
	}

    public ActivatedUpDownButton BtnBuy { get { return btnBuy; } }

    /// <summary>
    /// used to redirect common (not vip) user to vip shop
    /// </summary>
    public virtual bool IsProductVip
    {
        get { return _isProductVip; }
        set
        {
            _isProductVip = value;
            // show/hide vip icon
            if(sprVip==null) 
                Debug.LogWarning("sprVip was not set in BuyingBox.");
            else
                sprVip.SetActive(_isProductVip);
        }
    }
    
    public void SetButtonActivated (bool flag = true)
    {
        if (btnBuy) 
            btnBuy.Activated = flag;
    }

    private void SetPrice(ProfileInfo.Price price)
	{
		_price = price;

        if (_price == null)
        {
            sprSilver.SetActive(false);
            sprGold.SetActive(false);
            lblPrice.gameObject.SetActive(false);
            return;
        }

        lblPrice.gameObject.SetActive(true);
        switch (_price.currency)
        {
            case ProfileInfo.PriceCurrency.Silver:
                sprSilver.SetActive(true);
                sprGold.SetActive(false);
                break;
            case ProfileInfo.PriceCurrency.Gold:
                sprSilver.SetActive(false);
                sprGold.SetActive(true);
                break;
        }

        lblPrice.text = _price.LocalizedValue;
        lblPrice.SetMoneySpecificColorIfCan(_price);//Set MoneySpecificColor To label if inlineStyling option enabled.

        HorizontalLayout hl = lblPrice.transform.parent.GetComponent<HorizontalLayout>();
        if (hl)
        {
            //lblPrice.Commit();
            lblPrice.ForceBuild();
            hl.Align();
        }
    }
}
