using System;
using UnityEngine;

public class HangarRentBox : HangarBuyingBox
{
    [SerializeField]
    protected tk2dSlicedSprite sprBackground;

    [SerializeField]
    protected BonusStatsLabel bonusStatsLabel;

    [SerializeField]
    protected tk2dTextMesh lblBuy;

    [SerializeField]
    protected tk2dTextMesh lblBonusStats;
    
    [SerializeField]
    protected tk2dTextMesh lblBonusLifetime;

    [Header("Delta для изменения позиции вип иконки по вертикали по отношению к lblBuy")]
    [SerializeField] protected float sprVipVerticalOffset = 0;

    public virtual void SetBonusStatusText(Bodykit bodykit)
    {
        bonusStatsLabel.Show(bodykit);

        lblBonusLifetime.text = Localizer.GetText("ForHours", Clock.GetTimerString(Convert.ToInt64(bodykit.lifetime * 60 * 60), true));

        // Change background dimensions.
        sprBackground.dimensions
            = new Vector2(
                x: sprBackground.dimensions.x,
                y: bonusStatsLabel.SlicedSpriteDefaultYLength + (bonusStatsLabel.ExpandMultiplier + 1) * BonusStatsLabel.textLineInterval / sprBackground.scale.y);

        // Set "Buy" lable position.
        lblBuy.transform.localPosition
            = new Vector3(
                x: 0,
                y: sprBackground.transform.localPosition.y + (sprBackground.dimensions.y * sprBackground.scale.y) - BonusStatsLabel.textLineInterval * 0.5f,
                z: lblBuy.transform.localPosition.z);

        // Set expiration time position.
        lblBonusLifetime.transform.localPosition
            = new Vector3(
                x: 0,
                y: lblBuy.transform.localPosition.y - BonusStatsLabel.textLineInterval,
                z: lblBuy.transform.localPosition.z);

        // Set bonus stats description position.
        lblBonusStats.transform.localPosition
            = new Vector3(
                x: 0, 
                y: lblBonusLifetime.transform.localPosition.y - BonusStatsLabel.textLineInterval,
                z: lblBuy.transform.localPosition.z);

        // Show/hide vip icon.
        if (sprVip != null)
        {
            sprVip.SetActive(bodykit.isVip);

            sprVip.transform.localPosition
                            = new Vector3(
                                x: sprVip.transform.localPosition.x,
                                y: lblBuy.transform.localPosition.y + sprVipVerticalOffset,
                                z: sprVip.transform.localPosition.z);

            #region Старый метод форматирования
            //sprVip.transform.localPosition
            //        = new Vector3(
            //            x: -sprBackground.dimensions.x * sprBackground.scale.x / 2 + BonusStatsLabel.textLineInterval / (GameData.IsGame(Game.IronTanks) ? 2 : 1),
            //            y: sprBackground.dimensions.y * sprBackground.scale.y - BonusStatsLabel.textLineInterval / (GameData.IsGame(Game.IronTanks) ? 1 : 2),
            //            z: sprVip.transform.localPosition.z);
            #endregion
        }
    }
}
