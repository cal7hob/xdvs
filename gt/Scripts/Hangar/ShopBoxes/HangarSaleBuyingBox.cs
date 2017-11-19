using UnityEngine;
using System.Collections;

public class HangarSaleBuyingBox : HangarBuyingBox
{
	[SerializeField]
	private float saleRatio = 1;
	[SerializeField]
	private tk2dTextMesh lblOldPrice;
	[SerializeField]
	private GameObject sprOldSilver;
	[SerializeField]
	private GameObject sprOldGold;

	private tk2dTextMesh lblNewPrice;
	private GameObject sprNewSilver;
	private GameObject sprNewGold;

	public override ProfileInfo.Price Price
	{
		get
		{
			return base.Price;
		}
		set
		{ 
            if (VehicleOffersController.Instance && VehicleShop.Instance.IsInShop && VehicleOffersController.Instance.CheckIfItemOnSale(Shop.VehicleInView.Info.id))
            {
                OldPrice = value;
                ProfileInfo.Price salePrice = new ProfileInfo.Price(value);
                var offer = VehicleOffersController.Instance.Offers[Shop.VehicleInView.Info.id];
                salePrice = offer.DiscountPrice;
                base.Price = salePrice;
            }     
        }
	}
	
	private ProfileInfo.Price OldPrice
	{
		set
		{
			ChangeActivePrice(false);
			base.Price = new ProfileInfo.Price(value);
			ChangeActivePrice(true);
		}
	}

	void Awake()
	{
		lblNewPrice = lblPrice;
		sprNewSilver = sprSilver;
		sprNewGold = sprGold;
	}
	
	private void ChangeActivePrice(bool newPrice)
	{
		lblPrice = newPrice ? lblNewPrice : lblOldPrice;
		sprSilver = newPrice ? sprNewSilver : sprOldSilver;
		sprGold = newPrice ? sprNewGold : sprOldGold;
	}
}
