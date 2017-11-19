using UnityEngine;

public class FireButtonAR : FireButtonBase
{
    [Header("Настройки для AR")]

    [Header("Ссылки")]
    [SerializeField]
    private tk2dSprite sprIcon;
    [SerializeField]
    private tk2dSprite sprGlow;

    [Header("Цвета")]
    [SerializeField]
    private Color sprIconNormalColor;
    [SerializeField]
    private Color sprIconReloadingColor;

    protected override void FireButton_Down()
    {
        SetColor();
    }

    protected override void FireButton_Up()
    {
        SetColor();
    }

    protected override void OnReloaded(EventId id, EventInfo ei)
    {
        base.OnReloaded(id, ei);
        SetColor();
    }

    private void SetColor()
    {
        sprGlow.gameObject.SetActive(fireButton.IsPressed);
        sprIcon.color = IsReloading ? sprIconReloadingColor : sprIconNormalColor;
    }
    public override Rect Coord()
    {
        if (transform.GetComponentInChildren<tk2dSprite>() == null) return new Rect(0,0,0,0);
        var joyWorldTopRight = transform.TransformPoint(GetComponentInChildren<tk2dSprite>().GetBounds().max);
        var joyScreenTopRight = BattleGUI.Instance.GuiCamera.WorldToScreenPoint(joyWorldTopRight);
        var joyWorldBottomLeft = transform.TransformPoint(GetComponentInChildren<tk2dSprite>().GetBounds().min);
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

