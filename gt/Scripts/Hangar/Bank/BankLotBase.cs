using System;
using UnityEngine;

public class BankLotBase : ScrollableItem
{
    [SerializeField] protected tk2dSlicedSprite bgSprite;
    [SerializeField] protected tk2dUIItem btn;

    public override Vector2 Size
    {
        get { return new Vector2(bgSprite.dimensions.x * bgSprite.scale.x, bgSprite.dimensions.y * bgSprite.scale.y); }
    }

    public override void Initialize(params object[] parameters) { }

    public override void DestroySelf()
    {
    }

    public void SetBtnAction(Action<tk2dUIItem> action)
    {
        btn.OnClickUIItem += item => action(btn);
    }
}
