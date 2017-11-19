using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.GUI.Layouts;

public class HorizontalLayout : Layout
{

    public enum HorizontalPositions
    {
        Left,
        Center,
        Right
    }

    [Serializable]
    public class HorizontalAlignItem : Layout.AlignItem
    {
        public float paddingLeft = 0;
        public float paddingRight = 0;
    }

    public HorizontalPositions position;
    public List<HorizontalAlignItem> objectsToAlign;
    public bool OnHangarTickAlign = false;

    void Awake()
    {
        Dispatcher.Subscribe(EventId.HangarTimerTick,AlignHandler);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.HangarTimerTick, AlignHandler);
    }

    void AlignHandler(EventId _id, EventInfo _info)
    {
        if (OnHangarTickAlign)
        {
            Align();
        }
    }
    public override void Align()
    {
        if(alignOn == null)
        {
            Debug.LogError("Missing Reference 'alignOn' on object " + MiscTools.GetFullTransformName(transform));
            return;
        }

        var newPos = transform.localPosition;
        newPos.x = 0;
        transform.localPosition = newPos;

        float length = 0f;
        foreach (var item in objectsToAlign)
        {
            // В любом случае, здесь нужно проверять activeSelf
            if (!item.objectToAlign.gameObject.GetActive())
            {
                continue;
            }

            length += item.paddingLeft;
            length += item.objectToAlign.bounds.size.x;
            length += item.paddingRight;

        }

        float left = 0;
        switch (position)
        {
            case HorizontalPositions.Left:
                left = alignOn.bounds.min.x;
                break;
            case HorizontalPositions.Center:
                left = alignOn.bounds.center.x - (length / 2f);
                break;
            case HorizontalPositions.Right:
                left = alignOn.bounds.max.x - length;
                break;
        }
        foreach (var item in objectsToAlign)
        {
            if (!item.objectToAlign.gameObject.GetActive())
            {
                continue;
            }

            SnapTo(left + item.paddingLeft, item.objectToAlign);
            left += item.paddingLeft + item.objectToAlign.bounds.size.x + item.paddingRight;
        }
    }

    public override void SnapTo(float snapPos, Renderer obj)
    {
        var delta = obj.bounds.min.x - snapPos;
        var newPos = obj.transform.localPosition;
        newPos.x -= delta;
        obj.transform.localPosition = newPos;
    }
    ///Uncomment when realize the interface <IAligner>
    //public override List<GameObject> GetItemsList()
    //{
    //    return objectsToAlign.Select(go => go.GetGameObject()).ToList();
    //}
}
