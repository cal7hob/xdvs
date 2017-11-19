using UnityEngine;
using System.Collections.Generic;

public interface IAligner
{
    void Align();
    void AddItem(Renderer r, float paddingBefore, float paddingAfter, int position);
    List<GameObject> GetItemsList();
    bool IsVertical();
    void Clear();
}

public interface IAlignerItem
{
    GameObject GetGameObject();
}

/// <summary>
/// Класс для удобства накидывания ссылок на интерфейс IAligner (т.к. в Юнити нельзя сериализовать переменную типа Интерфейс), чтобы избежать во многих местах приведения MonoBehaviour к IAligner
/// </summary>
public abstract class UniAlignerBase : MonoBehaviour, IAligner
{
    public abstract void Align();
    public abstract void AddItem(Renderer r, float paddingBefore, float paddingAfter, int position = -1);
    public abstract List<GameObject> GetItemsList();
    public abstract bool IsVertical();
    public abstract void Clear();

    public virtual void OnEnable()
    {
        Align();
    }

    public virtual void Start()
    {
        foreach (var obj in GetItemsList())
        {
            if (obj != null)
            {
            }
        }

        Align();
    }

    protected virtual void OnDestroy ()
    {
        foreach (var obj in GetItemsList())
        {
            if (obj != null)
            {
            }
        }
    }

   
}
