using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VehicleParametersPanel : MonoBehaviour {

    public Dictionary<int, float> ArmorMax { get; private set; }
    public Dictionary<int, float> DamageMax { get; private set; }
    public Dictionary<int, float> SpeedMax { get; private set; }
    public Dictionary<int, float> ROFMax { get; private set; }

    private DeltaProgressBar armorBar;
    private DeltaProgressBar attackBar;
    private DeltaProgressBar speedBar;
    private DeltaProgressBar rofBar;

    private bool m_isInitialized = false;

    void Awake ()
    {
    }

    void Start ()
    {
        Init ();
    }

    void OnEnable ()
    {
    }

    void Init ()
    {
        if (m_isInitialized) {
            return;
        }

        Transform p = transform;
        Transform t = p.Find ("ArmorBar");
        // Первое включение дает ошибку и сразу же перезапускается и ищет нормально все О_о
        if (t == null) {
            return;
        }
        armorBar = t.GetComponent<DeltaProgressBar> ();
        attackBar = p.Find ("AttackBar").GetComponent<DeltaProgressBar> ();
        speedBar = p.Find ("SpeedBar").GetComponent<DeltaProgressBar> ();
        rofBar = p.Find ("ROFBar").GetComponent<DeltaProgressBar> ();

        m_isInitialized = true;
    }


    private void FillParameterDelta (DeltaProgressBar bar, float max, float prim, float sec)
    {
        bar.Max = max;
        bar.PrimaryValue = prim;
        bar.SecondaryValue = sec;
    }


    public void RecalcMaxStats (bool modulesForStats = true, bool camoForStats = false, bool decalForStats = false)
    {
        ArmorMax = new Dictionary<int, float> ();
        DamageMax = new Dictionary<int, float> ();
        ROFMax = new Dictionary<int, float> ();
        SpeedMax = new Dictionary<int, float> ();

        //Dictionary<int, List<TankInfo>> tankInfosToGroups = TankSelectors.GroupBy (tankSelector => (int)tankSelector.Value.userTank.info.tankGroup)
        //    .ToDictionary (
        //        group => (int)group.Key,
        //        group => group.ToList ().Select (tankSelector => tankSelector.Value.userTank.info).ToList ());

        //foreach (KeyValuePair<int, List<TankInfo>> tankInfosToGroup in tankInfosToGroups) {
        //    float baseArmorMax = SafeLinq.Max (tankInfosToGroup.Value.Select (tankInfo => (float)tankInfo.baseArmor));
        //    float baseDamageMax = SafeLinq.Max (tankInfosToGroup.Value.Select (tankInfo => (float)tankInfo.baseDamage));
        //    float baseROFMax = SafeLinq.Max (tankInfosToGroup.Value.Select (tankInfo => (float)tankInfo.baseROF));
        //    float baseSpeedMax = SafeLinq.Max (tankInfosToGroup.Value.Select (tankInfo => (float)tankInfo.baseSpeed));

        //    float armorMax = baseArmorMax;
        //    float damageMax = baseDamageMax;
        //    float rofMax = baseROFMax;
        //    float speedMax = baseSpeedMax;

        //    if (modulesForStats) {
        //        List<UserTank> fullUpgradedTanks = tankInfosToGroup.Value.Select (
        //            tankInfo => new UserTank (
        //                tankInfo,
        //                TankUpgrades.GetFullModuleUpgrades (tankInfo))).ToList ();

        //        armorMax = SafeLinq.Max (fullUpgradedTanks.Select (upgradedTank => upgradedTank.Armor));
        //        damageMax = SafeLinq.Max (fullUpgradedTanks.Select (upgradedTank => upgradedTank.Damage));
        //        rofMax = SafeLinq.Max (fullUpgradedTanks.Select (upgradedTank => upgradedTank.RoF));
        //        speedMax = SafeLinq.Max (fullUpgradedTanks.Select (upgradedTank => upgradedTank.Speed));
        //    }

        //    if (camoForStats || GUIPager.ActivePage == "PatternShop") {
        //        //armorMax = armorMax + SafeLinq.Max (patternSelectors.Select (patternSelector => (float)patternSelector.Value.pattern.armorGain)) * baseArmorMax;
        //        //damageMax = damageMax + SafeLinq.Max (patternSelectors.Select (patternSelector => (float)patternSelector.Value.pattern.damageGain)) * baseDamageMax;
        //        //rofMax = rofMax + SafeLinq.Max (patternSelectors.Select (patternSelector => (float)patternSelector.Value.pattern.rofGain)) * baseROFMax;
        //        //speedMax = speedMax + SafeLinq.Max (patternSelectors.Select (patternSelector => (float)patternSelector.Value.pattern.speedGain)) * baseSpeedMax;
        //    }

        //    if (decalForStats || GUIPager.ActivePage == "DecalShop") {
        //        //armorMax = armorMax + SafeLinq.Max (decalSelectors.Select (decalSelector => (float)decalSelector.Value.decal.armorGain)) * baseArmorMax;
        //        //damageMax = damageMax + SafeLinq.Max (decalSelectors.Select (decalSelector => (float)decalSelector.Value.decal.damageGain)) * baseDamageMax;
        //        //rofMax = rofMax + SafeLinq.Max (decalSelectors.Select (decalSelector => (float)decalSelector.Value.decal.rofGain)) * baseROFMax;
        //        //speedMax = speedMax + SafeLinq.Max (decalSelectors.Select (decalSelector => (float)decalSelector.Value.decal.speedGain)) * baseSpeedMax;
        //    }

        //    ArmorMax.Add (tankInfosToGroup.Key, armorMax);
        //    DamageMax.Add (tankInfosToGroup.Key, damageMax);
        //    ROFMax.Add (tankInfosToGroup.Key, rofMax);
        //    SpeedMax.Add (tankInfosToGroup.Key, speedMax);
        //}
    }

}
