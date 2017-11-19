using System;
using UnityEngine;
using System.Collections;

public class StatTableFlagGroup : MonoBehaviour {

    public tk2dSlicedSprite sprFlag;
    public tk2dSlicedSprite sprKiller;
    public StatTableRow statTableRow;

    private bool offender;

    public Vector3 FlagExtents { get; private set; }
    public Vector3 SprKillerNormalPosition { get; private set; }

    public bool Offender
    {
        get { return offender; }
        set
        {
            if (offender == value)
                return;

            offender = value;
            if (sprKiller)
                sprKiller.gameObject.SetActive(offender);
        }
    }

    public string Flag
    {
        set
        {
            if (!statTableRow.IsWorking || sprFlag == null)
                return;

            sprFlag.SetSprite(
                string.IsNullOrEmpty(value) || value.ToLower() == "unknown" 
                    ? GameData.UNKNOWN_FLAG_NAME
                    : value);
        }
    }

    void Awake()
    {
        FlagExtents = sprFlag.GetComponent<Renderer>().bounds.extents;
        SprKillerNormalPosition = sprKiller.transform.localPosition;
    }

    public void SetFlag(PlayerStat stat)
    {
        if (stat == null || string.IsNullOrEmpty(stat.countryCode) ||
                (BattleController.allVehicles.ContainsKey(stat.playerId) && BattleController.allVehicles[stat.playerId].data.hideMyFlag) ||
                (ProfileInfo.AvatarOption == AvatarOption.showNothing && stat.playerId != BattleController.MyPlayerId) ||
                (ProfileInfo.AvatarOption == AvatarOption.showOnlyAvatars))
        {
            sprFlag.gameObject.SetActive(false);
            if(statTableRow.useFlagGroupAlignment)
                sprKiller.transform.localPosition = new Vector3(FlagExtents.x, sprKiller.transform.localPosition.y);
        }
        else
        {
            Flag = BattleController.allVehicles.ContainsKey(stat.playerId) ? (string)BattleController.allVehicles[stat.playerId].data.country : stat.countryCode;
            sprFlag.gameObject.SetActive(true);
            if (statTableRow.useFlagGroupAlignment)
                sprKiller.transform.localPosition = SprKillerNormalPosition;
        }
    }
}
