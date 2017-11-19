using UnityEngine;

public abstract class ShopItemPool<TShopItem> : MonoBehaviour
    where TShopItem : class, IShopItem
{
    private TShopItem[] items;

    public static ShopItemPool<TShopItem> Instance
    {
        get; private set;
    }

    public abstract BodykitInEditor[] ReferencedBodykits { get; }

    /// <summary>
    /// ����� �������� � ��������� �������. �� ������������ ����������! ��� ����� ���� GetItemById().
    /// </summary>
    public TShopItem[] Items
    {
        get { return items = items ?? GetItems(); }
    }

    void Awake()
    {
        Instance = this;
    }

    public TShopItem GetItemById(int id)
    {
        foreach (TShopItem item in Items)
            if (item.Id == id)
                return item;

        return null;
    }

    protected abstract TShopItem[] GetItems();
}
