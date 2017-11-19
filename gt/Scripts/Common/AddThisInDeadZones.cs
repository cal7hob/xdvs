using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddThisInDeadZones : AbstractClassForButtons
{
    public tk2dSprite boundingSprite;

    public override Rect Coord()
    {
        var joyWorldTopRight = boundingSprite.transform.TransformPoint(boundingSprite.GetBounds().max);
        var joyScreenTopRight = BattleGUI.Instance.GuiCamera.WorldToScreenPoint(joyWorldTopRight);
        var joyWorldBottomLeft = boundingSprite.transform.TransformPoint(boundingSprite.GetBounds().min);
        var joyScreenBottomLeft = BattleGUI.Instance.GuiCamera.WorldToScreenPoint(joyWorldBottomLeft);

        var Area = new Rect
        {
            xMin = joyScreenBottomLeft.x,
            yMin = joyScreenBottomLeft.y,
            xMax = joyScreenTopRight.x,
            yMax = joyScreenTopRight.y,
        };
        return Area;
    }

}
