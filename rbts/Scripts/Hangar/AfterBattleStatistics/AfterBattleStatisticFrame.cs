using UnityEngine;

public class AfterBattleStatisticFrame : MonoBehaviour
{
    public float interval = 60;

    public BattleStatisticsFrameRow battleStatisticsFrameRow;
    public BattleStatisticsFrameRow DamageRow { get; set; }
    public BattleStatisticsFrameRow ShotsAmountRow { get; set; }
    public BattleStatisticsFrameRow HitsAmountRow { get; set; }
    public BattleStatisticsFrameRow AccuracyRow { get; set; }
    public BattleStatisticsFrameRow FragsRow { get; set; }
    public BattleStatisticsFrameRow MileageRow { get; set; }
    public BattleStatisticsFrameRow DeathsRow { get; set; }

    public Transform columnWrapper;
    public GameObject lblBattleStatistics;

    private int _damage;
    private int _shotsAmount;
    private int _hitsAmount;
    private int _accuracy;
    private int _frags;
    private int _mileage;
    private int _deaths;

    private Vector3 nextRowLocalPos = Vector3.zero;

    public bool NotEmpty { get; private set; }

    public BattleStatisticsFrameRow CreateRow(int statValue, string localizationKey)
    {   
        var row = Instantiate(battleStatisticsFrameRow);
        row.statValue.text = statValue.ToString("N0", GameData.instance.cultureInfo.NumberFormat);
        row.statFieldName.text = Localizer.GetText(localizationKey);
        row.transform.SetParent(columnWrapper, false);
        

        if(statValue > 0)
        {
            NotEmpty = true;
            row.transform.localPosition = nextRowLocalPos;
            nextRowLocalPos -= new Vector3(0, interval, 0);
        }
        else
        {
            row.gameObject.SetActive(false);
        }

        return row;
    }

    public void FillStatisticFrame()
    {
        bool countRocketShots = GameData.IsGame(Game.BattleOfWarplanes | Game.BattleOfHelicopters | Game.ApocalypticCars);
        var br = Http.Manager.BattleServer.result;

        AccuracyRow = CreateRow(countRocketShots ? br.AccuracySaclos : br.Accuracy, "lblAccuracyInBattle");
        DeathsRow = CreateRow(br.deaths, "lblDeathsInBattle");
        FragsRow = CreateRow(br.frags, "lblFragsInBattle");
        HitsAmountRow = CreateRow(countRocketShots ? br.hitsSaclos : br.hitsSaclos, "lblHitsInBattle");
        MileageRow = CreateRow((int)br.mileage, "lblMileageInBattle");
        ShotsAmountRow = CreateRow(countRocketShots ? br.shootsSaclos : br.shoots, "lblShootsInBattle");

        DamageRow = CreateRow(br.givenDamage + br.givenDamageSaclos, "lblGivenDemage");

        // update overall statistics
        BattleStatisticsManager.CalcOverallBattleStatistics();
    }

    ///// <summary>
    ///// done damage to opponents
    ///// </summary>
    //public int Damage
    //{
    //    get { return _damage; }
    //    set { SetValue(ref _damage, value); }
    //}

    ///// <summary>
    ///// amount of shots made in battle
    ///// </summary>
    //public int ShotsAmount
    //{
    //    get { return _shotsAmount; }
    //    set { SetValue(ref _shotsAmount, value); }
    //}

    ///// <summary>
    ///// amount of succeeded shots
    ///// </summary>
    //public int HitsAmount
    //{
    //    get { return _hitsAmount; }
    //    set { SetValue(ref _hitsAmount, value); }
    //}

    ///// <summary>
    ///// accuracy in battle
    ///// </summary>
    //public int Accuracy
    //{
    //    get { return _accuracy; }
    //    set { SetValue(ref _accuracy, value); }
    //}

    ///// <summary>
    ///// amount of killed opponents
    ///// </summary>
    //public int Frags
    //{
    //    get { return _frags; }
    //    set { SetValue(ref _frags, value); }
    //}

    ///// <summary>
    ///// amount of driven miles in battle
    ///// </summary>
    //public int Mileage
    //{
    //    get { return _mileage; }
    //    set { SetValue(ref _mileage, value); }
    //}

    ///// <summary>
    ///// amount of player deaths in battle
    ///// </summary>
    //public int Deaths
    //{
    //    get { return _deaths; }
    //    set { SetValue(ref _deaths, value); }
    //}


    ///// <summary>
    ///// check if value was changed and assign it for textMesh
    ///// </summary>
    ///// <param name="prop">reference to field to be changed</param>
    ///// <param name="value">new value</param>
    ///// <param name="guiMesh">tk2dTextMesh to be updated</param>
    //void SetValue(ref int prop, int value)
    //{
    //    if (Equals(prop, value)) return;
    //    prop = value;
    //}

    public void CheckIfFrameIsEmpty()
    {
        lblBattleStatistics.SetActive(NotEmpty);
        gameObject.SetActive(NotEmpty);
    }
}
