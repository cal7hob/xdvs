using UnityEngine;
using System.Collections;
using System.Linq;

public class SpecialOfferPrefab : MonoBehaviour
{
    public PriceRenderer priceRenderer;
    public OldPriceRenderer oldPriceRenderer;
    public tk2dBaseSprite sprProduct;
    public tk2dTextMesh timer;
    public tk2dTextMesh lblBuy;
    public tk2dUIItem btnBuy;
    public tk2dTextMesh info;

    public void Hide()
    {
        if(gameObject.activeSelf)
        {
            gameObject.SetActive(false);
            SpecialOffersPage.Instance.FramesReposition();
        }
    }

    public void Show()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            SpecialOffersPage.Instance.FramesReposition();
        }
    }   
}
