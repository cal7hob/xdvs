using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public interface IItemsFactory : IFactory
{
    IItem Create(params object[] initParams);
    void CreateAll(IEnumerable allData, bool destroyAllIfNotEmpty = true, ParamDict commonParameters = null);//Create all items by data collection
    Transform Parent { get;}
    GameObject Prefab { get;}
    bool IsVertical { get; }
    tk2dUILayout MainLayout { get; }
    IItem GetItemByUniqId(object id);//int or string...
    void SimulateClickItem(string id);
}

public interface IFactory
{
}

public interface IItem
{
    Vector2 GetSize();
    void Initialize(object[] parameters);
    void DesrtoySelf();
    string GetUniqId { get; }//Unique Id for quick search in Factory items
    tk2dUIItem MainUIItem { get; }
    Transform MainTransform { get; }//top item transform
    void UpdateElements();
}

public interface IInventoryItem: IItem
{
    int ContentId { get; set; }
    bool IsEmpty { get; }
}

/// <summary>
/// Зона при клике по которой не работает двойной тап и поворот башни
/// </summary>
public interface IDeadZone
{
    Rect GetDeadZone();
}

public interface IGunSight
{
    void ShowTargetGunSight(Vector3 position, float distance = 0);
    void HideTargetGunSight();
    void ShowStaticGunSight(Vector3 position);
    void HideStaticGunSight();
    IProgressBar TargetLockedProgressBar { get; }
}

public interface IQueueablePage
{
    void BeforeActivation();
    void Activated();
}

public interface IActivated
{
    bool Activated { get; set; }
}
