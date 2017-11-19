using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ConsumableInventoryItem : MonoBehaviour
{
    [SerializeField] private GameObject countWrapper;
    [SerializeField] private tk2dTextMesh lblCount;
    [SerializeField] private tk2dBaseSprite sprite;
    [SerializeField] private string emptyCellSpriteName;

    private int id = -1;
    public int Id
    {
        get { return id;}
        set
        {
            id = value;
            UpdateSlot();
        }
    }

    public bool IsEmpty { get { return id < 0; } }

    public void UpdateSlot()
    {
        var spriteName = IsEmpty ? emptyCellSpriteName : GameData.consumableInfos[id].icon;
        sprite.SetSprite(spriteName);
        countWrapper.SetActive(!IsEmpty);
        if (!IsEmpty)
            lblCount.text = Mathf.Clamp(ProfileInfo.consumableInventory[id], 0, GameData.consumableInfos[id].maxInBattle).ToString();
    }
}
