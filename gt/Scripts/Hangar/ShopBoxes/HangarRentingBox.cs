using UnityEngine;

public class HangarRentingBox : HangarRentBox
{
    private float progress;

    [SerializeField]
    private tk2dSlicedSprite progressBar;
    [SerializeField]
    private tk2dTiledSprite progressBarTeam;
    [SerializeField]
    private tk2dTextMesh lblTimeRemain;
    [SerializeField]
    private float fullProgressWidth;
    [SerializeField]
    private float minFillerLength = 0;

    /*	UNITY SECTION	*/
    void Start()
    {
        if (fullProgressWidth < 1)
            fullProgressWidth = progressBar != null
                ? progressBar.dimensions.x
                : progressBarTeam != null
                    ? progressBarTeam.dimensions.x
                    : 0;
    }

    /*	PUBLIC SECTION	*/
    public float Progress
    {
        get { return progress; }
        set
        {
            progress = Mathf.Clamp01(value);
            float val = fullProgressWidth * progress;
            val = Mathf.Clamp(val, minFillerLength, val);
            progressBarTeam.dimensions = new Vector2(val, progressBarTeam.dimensions.y);
        }
    }

    public long Remain { set { SetTimeRemainText(value); } }

    public override void SetBonusStatusText(Bodykit bodykit)
    {
        bonusStatsLabel.Show(bodykit);
    }

    /*	PRIVATE SECTION	*/
    private void SetTimeRemainText(long remain)
    {
        if(remain < 0)
            return;

        lblTimeRemain.text = Clock.GetTimerString(remain);
    }
}