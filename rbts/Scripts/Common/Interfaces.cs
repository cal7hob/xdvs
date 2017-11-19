using UnityEngine;

public interface IItemsFactory : IFactory
{
    IItem Create(params object[] initParams);
    Transform Parent { get; }
    GameObject Prefab { get; }
    bool IsVertical { get; }
    tk2dUILayout MainLayout { get; }
    IItem GetItemByUniqId(string id);
}

public interface IFactory
{
}

public interface IItem
{
    Vector2 GetSize();
    void Initialize(object[] parameters);
    void DestroySelf();
    string GetUniqId { get; }//Unique Id for quick search in Factory items
    tk2dUIItem MainUIItem { get; }
    Transform MainTransform { get; }//top item transform
}

/// <summary>
/// Зона, при клике по которой не работает двойной тап и поворот башни
/// </summary>
public interface IDeadZone
{
    Rect GetDeadZone();
}
