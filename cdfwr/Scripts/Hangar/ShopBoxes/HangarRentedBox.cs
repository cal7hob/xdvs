using UnityEngine;

public class HangarRentedBox : HangarRentingBox
{
    public override void SetBonusStatusText(Bodykit bodykit)
    {
        bonusStatsLabel.Show(bodykit);
    }
}
