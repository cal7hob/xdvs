using UnityEngine;

public class ConsumableKitItem : ScrollableItem
{
    public GameObject GameObject { get { return gameObject; } }
    public Transform Transform { get { return transform; } }

    public tk2dSlicedSprite sizeBg;//для определения размера итема

    [SerializeField] private tk2dTextMesh lblDescription;
    [SerializeField] private tk2dBaseSprite sprite;//специфичная для итема текстура
    [SerializeField] private PriceRenderer setPriceScript;
    [SerializeField] private ActivatedUpDownButton btnBuy;

    public int ConsumableKitId { get; private set; }

    private void OnClick(tk2dUIItem btn)
    {
        switch(btn.name)
        {
            case "btnBuy":
                //ConsumablesPage.Instance.BuyConsumable(ConsumableId, () => { btnBuy.Activated = false; }, () => { btnBuy.Activated = true; UpdateComponents(); });
                break;
        }
    }


    public override void Initialize(params object[] parameters)
    {
        ConsumableKitId = (int)parameters[0];

        UpdateComponents();
    }

    private void UpdateComponents()
    {
        //lblDescription.text = string.Format("id = {0}, sprite = {1}", ConsumableKitId, GameData.consumableInfos[ConsumableKitId].icon);
        //setPriceScript.Price = GameData.consumableInfos[ConsumableKitId].price.ToPrice();
        //HelpTools.SetSpriteToAllSpritesInCollection(sprite, GameData.consumableInfos[ConsumableKitId].icon);
    }

    public override Vector2 Size
    {
        get { return new Vector2(sizeBg.dimensions.x * sizeBg.scale.x, sizeBg.dimensions.y * sizeBg.scale.y); } 
    }
}
