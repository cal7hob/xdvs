using UnityEngine;

public class SpecialOfferPrefab : MonoBehaviour
{
    public PriceRenderer priceRenderer;
    public OldPriceRenderer oldPriceRenderer;
    public tk2dBaseSprite sprProduct;
    public tk2dSpriteCollectionData sprCollection;
    public tk2dTextMesh timer;
    public tk2dTextMesh lblBuy;
    public tk2dUIItem btnBuy;
    public tk2dTextMesh info;  

    public void SetActive(bool activate) 
    {
        if (gameObject == null || gameObject.activeSelf == activate)
        {
            return;
        }

        gameObject.SetActive(activate);
        SpecialOffersPage.Instance.FramesReposition();
    }
}
