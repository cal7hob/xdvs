using UnityEngine;
using System;

public class HangarDeliverBox : HangarBuyingBox
{
    [SerializeField] private tk2dSlicedSprite progressBar;
    [SerializeField] private tk2dTiledSprite progressBarTeam;
    [SerializeField] private tk2dTextMesh lblTimeRemain;
    [SerializeField] private float fullProgressWidth;
    [SerializeField] private float minFillerLength = 0;
    [SerializeField] private GameObject btnModuleDeliveryForAdsViewing;
    [SerializeField] private InterfaceExtensions.ConditionHelper btnModuleDeliveryForAdsViewingConditionHelper; //do something when btn appears

    private float progress;

    public float Progress
    {
        get { return progress; }
        set
        {
            progress = Mathf.Clamp01(value);
            float val = fullProgressWidth * progress;
            val = Mathf.Clamp(val, minFillerLength, val);
            if (GameData.IsGame(Game.IronTanks | Game.ToonWars | Game.BattleOfHelicopters))
                progressBar.dimensions = new Vector2(val, progressBar.dimensions.y);
            else
                progressBarTeam.dimensions = new Vector2(val, progressBarTeam.dimensions.y);
        }
    }

    public long Remain { set { SetTimeRemainText(value); } }

    void Start()
    {
        if (fullProgressWidth < 1)
            fullProgressWidth = GameData.IsGame(Game.IronTanks | Game.ToonWars | Game.BattleOfHelicopters) && progressBar != null
                ? progressBar.dimensions.x
                : progressBarTeam != null 
                    ? progressBarTeam.dimensions.x 
                    : 0;

        HangarController.OnTimerTick += OnTimerTick;
    }

    private void OnDestroy()
    {
        HangarController.OnTimerTick -= OnTimerTick;
    }

    private void OnEnable()
    {
        UpdateBtnModuleDeliveryForAdsViewingState();
    }

    private void OnTimerTick(double time)
    {
        UpdateBtnModuleDeliveryForAdsViewingState();
    }

    private void UpdateBtnModuleDeliveryForAdsViewingState()
    {
        if (!gameObject.activeInHierarchy)
            return;
        btnModuleDeliveryForAdsViewing.SetActive(ProfileInfo.IsNeededToShowBtnModuleDeliveryForAdsViewing);
        if (btnModuleDeliveryForAdsViewingConditionHelper)
            btnModuleDeliveryForAdsViewingConditionHelper.State = Convert.ToInt32(btnModuleDeliveryForAdsViewing.activeSelf);
    }

    private void SetTimeRemainText(long remain)
    {
        if (remain < 0)
            return;
        lblTimeRemain.text = Clock.GetTimerString(remain);
    }

    public void BtnModuleDeliveryForAdsViewingOnClick(tk2dUIItem btn)
    {
        RewardedVideoController.Instance.BtnDeliveryOnClick();
    }
}
