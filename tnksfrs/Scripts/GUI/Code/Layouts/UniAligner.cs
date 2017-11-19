using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.GUI.Layouts;
using System.Linq;

public class UniAligner : UniAlignerBase
{
    public enum Positions
    {
        Left,
        Center,
        Right
    }

    [Serializable]
    public class UniAlignerItem:IAlignerItem
    {
        public Renderer objectToAlign;
        public float paddingBefore = 0;
        public float paddingAfter = 0;

        public UniAlignerItem(Renderer _renderer, float _paddingBefore, float _paddingAfter)
        {
            objectToAlign = _renderer;
            paddingBefore = _paddingBefore;
            paddingAfter = _paddingAfter;
        }

        public Vector3 Size
        {
            get
            {
                if (objectToAlign)
                    return objectToAlign.bounds.size;
                
                else
                    return Vector3.zero;
            }
            
        }

        public GameObject gameObject
        {
            get
            {
                    return objectToAlign.gameObject;
                
            }
        }

        /// <summary>
        /// Interface realization
        /// </summary>
        public GameObject GetGameObject()
        {
            return gameObject;
        }

        public Vector3 MinBounds
        {
            get
            {
                
                    return objectToAlign.bounds.min;
                
            }
        }

        public Vector3 MaxBounds
        {
            get
            {
                
                    return objectToAlign.bounds.max;
                
            }
        }
    }

    public Renderer alignOn;
    public Positions position;
    public bool isVertical = false;
    public List<UniAlignerItem> objectsToAlign = new List<UniAlignerItem>();

    public override void Align()
    {
        if (alignOn == null)
        {
            Debug.LogError("Missing Reference 'alignOn' on object " + MiscTools.GetFullTransformName(transform));
            return;
        }
        //Debug.LogError("Align() object " + MiscTools.GetFullTransformName(transform));
        var newPos = transform.localPosition;
        if(isVertical)
            newPos.y = 0;
        else
            newPos.x = 0;
        transform.localPosition = newPos;

        float length = 0f;
        foreach (var item in objectsToAlign)
        {
            // В любом случае, здесь нужно проверять activeSelf
            if (!item.gameObject.GetActive())
                continue;

            length += item.paddingBefore;
            length += isVertical ? item.Size.y : item.Size.x;
            length += item.paddingAfter;
        }

        float left = 0;
        switch (position)
        {
            case Positions.Left:
                left = isVertical ?
                    alignOn.bounds.max.y :
                    alignOn.bounds.min.x;
                break;
            case Positions.Center:
                left = isVertical ? 
                    (alignOn.bounds.center.y + (length / 2f) ) :
                    (alignOn.bounds.center.x - (length / 2f));
                break;
            case Positions.Right:
                left = isVertical ? 
                    (alignOn.bounds.min.y + length) :
                    (alignOn.bounds.max.x - length);
                break;
        }
        foreach (var item in objectsToAlign)
        {
            if (!item.gameObject.GetActive())
                continue;

            if(isVertical)
            {
                SnapTo(left - item.paddingBefore, item.MaxBounds, item.gameObject);//По оси Y координата возрастает вверх, а нам требуется чтобы вниз
                left -= item.paddingBefore + item.Size.y + item.paddingAfter;
            }
            else
            {
                SnapTo(left + item.paddingBefore, item.MinBounds, item.gameObject);
                left += item.paddingBefore + item.Size.x + item.paddingAfter;
            }
        }
    }

    public void SnapTo(float snapPos, Vector3 bounds, GameObject obj)
    {
        var delta = (isVertical ? bounds.y : bounds.x) - snapPos;
        var newPos = obj.transform.localPosition;
        if(isVertical)
            newPos.y -= delta;
        else
            newPos.x -= delta;
        obj.transform.localPosition = newPos;
    }


    /// <summary>
    /// Добавление итема в список выравнивания. По умолчанию добавляем в конец списка (position = -1)
    /// </summary>
    public override void AddItem(Renderer r, float paddingBefore, float paddingAfter, int position = -1)
    {
        if (objectsToAlign == null)
            objectsToAlign = new List<UniAlignerItem>();
        if(r == null)
        {
            Debug.LogError("Cant add item to UniAligner! Renderer is NULL.");
            return;
        }
        if (position == -1)
            position = objectsToAlign.Count;
        objectsToAlign.Insert(position, new UniAlignerItem(r, paddingBefore, paddingAfter));
    }

    public override List<GameObject> GetItemsList()
    {
        return objectsToAlign.Select(go => go.GetGameObject()).ToList();
    }

    public override bool IsVertical()
    {
        return isVertical;
    }

    public override void Clear()
    {
        objectsToAlign.Clear();
    }
}
