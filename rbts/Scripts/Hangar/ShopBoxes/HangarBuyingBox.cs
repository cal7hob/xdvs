using UnityEngine;

public class HangarBuyingBox : MonoBehaviour
{
    [SerializeField]
	protected ActivatedUpDownButton btnBuy;

	private ProfileInfo.Price _price;

	[SerializeField]
	protected tk2dTextMesh lblPrice;
	[SerializeField]
	protected GameObject sprSilver;
	[SerializeField]
	protected GameObject sprGold;
    [SerializeField] 
    protected GameObject sprVip;

	public virtual ProfileInfo.Price Price
	{
		get { return _price; }
		set { SetPrice(new ProfileInfo.Price(value)); }
	}

    public ActivatedUpDownButton BtnBuy { get { return btnBuy; } }

    private void OnEnable()
    {
        if (sprVip != null)
            sprVip.SetActive(IsProductVip);
    }

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
    private bool _isProductVip;



    public void SetButtonActivated (bool flag = true)
    {
        if (btnBuy != null) {
            btnBuy.Activated = flag;
        }
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

        lblPrice.text = _price.value.ToString("N0", GameData.instance.cultureInfo.NumberFormat);
        _price.SetMoneySpecificColorIfCan(lblPrice);//Set MoneySpecificColor To label if inlineStyling option enabled.

        TextTie textTieScript = lblPrice.GetComponent<TextTie>();
        if (textTieScript != null)
            textTieScript.SetText(lblPrice.text);//В этом скрипте не метода выравнивания без присваивания текста. И вообще он не работает
        HorizontalLayout hl = lblPrice.transform.parent.GetComponent<HorizontalLayout>();
        if (hl)
        {
            //lblPrice.Commit();
            lblPrice.ForceBuild();
            hl.Align();
        }
            
    }
}
