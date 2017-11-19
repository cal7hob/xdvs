using UnityEngine;

public class ConsumablesInventoryItem : MonoBehaviour
{
    [SerializeField] private GameObject countWrapper;
    [SerializeField] private tk2dTextMesh lblCount;
    [SerializeField] private tk2dBaseSprite sprite;//специфичная для итема текстура
    [SerializeField] private string emptyCellSpriteName;

    private int id = -1;
    public int Id {
        get { return id; }
        set
        {
            id = value;
            UpdateElements();
        }
    }

    public int Count
    {
        get
        {
           return Mathf.Clamp(ProfileInfo.consumableInventory[id], 0, GameData.consumableInfos[id].maxInBattle);
        }
    }

    public bool IsEmpty { get { return id < 0; } }

    private void OnEnable()
    {
         GetComponent<tk2dUIItem>().OnClick += OnItemClick;
    }

    private void OnDisable()
    {
         GetComponent<tk2dUIItem>().OnClick -= OnItemClick;
    }

    public void UpdateElements()
    {
        sprite.SetSprite(IsEmpty ? emptyCellSpriteName : GameData.consumableInfos[id].GetIcon(false));

        if (GameData.IsGame(Game.FTRobotsInvasion))
        {
            if (IsEmpty)
                sprite.color = new Color32(140, 214, 254, 255);
            else
                sprite.color = Color.white;
        }

        countWrapper.SetActive(!IsEmpty);

        if (!IsEmpty)
            lblCount.text = Count.ToString();
    }

    private void OnItemClick()
    {
        if(!IsEmpty)
            Messenger.Send(EventId.ChangeConsumableInventoryState, new EventInfo_IB(id, false));
    }
}
