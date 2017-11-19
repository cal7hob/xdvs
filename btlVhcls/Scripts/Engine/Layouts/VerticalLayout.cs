using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.GUI.Layouts;

public class VerticalLayout : Layout {

    public enum VerticalPositions
    {
        Top,
        Middle,
        Bottom
    }

    [Serializable]
    public class VerticalAlignItem : Layout.AlignItem
    {
        public float paddingTop = 0;
        public float paddingBottom = 0;
    }

    public VerticalPositions position;
    public List<VerticalAlignItem> objectsToAlign;

    public override void Align()
    {
        var newPos = transform.localPosition;
        newPos.y = 0;
        transform.localPosition = newPos;

        float length = 0f;
        foreach (var item in objectsToAlign)
        {
            // В любом случае, здесь нужно проверять activeSelf
            if (!item.objectToAlign.gameObject.GetActive())
            {
                continue;
            }

            length += item.paddingTop;
            length += item.objectToAlign.bounds.size.y;
            length += item.paddingBottom;

        }

        float top = 0;
        switch (position)
        {
            case VerticalPositions.Top:
                top = alignOn.bounds.max.y - length;
                break;
            case VerticalPositions.Middle:
                top = alignOn.bounds.center.y - (length / 2f);
                break;
            case VerticalPositions.Bottom:
                top = alignOn.bounds.min.y;
                break;
        }

        for (int i = objectsToAlign.Count - 1; i >= 0; i--)
        {
            var item = objectsToAlign[i];

            if (!item.objectToAlign.gameObject.GetActive())
            {
                continue;
            }

            SnapTo(top + item.paddingTop, item.objectToAlign);
            top += item.paddingTop + item.objectToAlign.bounds.size.y + item.paddingBottom;
        }
    }

    public override void SnapTo(float snapPos, Renderer obj)
    {
        var delta = obj.bounds.min.y - snapPos;
        var newPos = obj.transform.localPosition;
        newPos.y -= delta;
        obj.transform.localPosition = newPos;
    }
}
