using UnityEngine;

public class StatTableRow: MonoBehaviour
{
	public tk2dTextMesh lblRank;
	public tk2dTextMesh lblPlayerName;
	public tk2dTextMesh lblScore;
	public tk2dTextMesh lblKills;
	public tk2dTextMesh lblDeaths;
	public tk2dSlicedSprite sprPremiumIcon;
    public tk2dSlicedSprite sprGoldRushLeader;
    public StatTableFlagGroup flagGroup;
    public bool useFlagGroupAlignment = false;
    public float lblPlayerNameMaxWidth = 400;
    [Header("Цвет имени клана, когда оно пишется в том же текстмеше, что и имя плеера ")]
    public Color clanNameColor = new Color(1,1,0.09f,1);
    public string clanNameFormatString = " {0}[{1}]";
    [Header("Если используется отдельный текстмеш для имени клана")]
    public tk2dTextMesh lblClanName;
    public float lblClanNameMaxWidth = 400;
    [Header("Окраска бэков через строчку")]
    public ActivatedUpDownButton interlacedBackgrounds;//Activated Object - четный, Disactivated - нечетные строчки
    public MeshRenderer hideThisBgInTeamMode;//If this bg is defined - hide it in team mode, because stat table has its own bg

    public bool IsWorking { get; private set; }

	private void Awake()
	{
		if (!lblRank || !lblPlayerName || !lblScore || !lblKills || !lblDeaths)
		{
			DT.LogError(gameObject, "Not enough data in StatTableRow. Disabled.");
			IsWorking = false;
			return;
		}
        if (lblClanName)
            lblClanName.text = "";
        if (flagGroup && flagGroup.sprKiller)
            flagGroup.sprKiller.gameObject.SetActive(false);
        if (hideThisBgInTeamMode && BattleController.Instance.IsTeamMode)
            hideThisBgInTeamMode.enabled = false;
        IsWorking = true;
	}
	
    public int Rank
	{
		set
		{
			if (!IsWorking)
				return;
            lblRank.text = value.ToString(GameData.instance.cultureInfo.NumberFormat);
		}
	}

	public string PlayerName
	{
        get
        { return lblPlayerName.text; }
		set
		{
			if (!IsWorking || lblPlayerName == null)
				return;
            lblPlayerName.text = value;
            HelpTools.ClampLabelText(lblPlayerName, lblPlayerNameMaxWidth);
		}
	}

    /// <summary>
    /// Имя клана должно присваиваться после имени игрока, для совместимости со старыми проектами,
    /// в которых имя клана пишется в текстмеш lblPlayerName
    /// </summary>
    public string ClanName
    {
        set
        {
            if (!IsWorking || value == null)
                return;

            if(lblClanName != null)//Если для имени клана используется отдельный текстмеш
            {
                if (value.Length > 0)
                {
                    lblClanName.text = string.Format(clanNameFormatString, "", value);//Здесь цвет можно не указывать, т.к. можно поменять цвет в самом текст меше
                    HelpTools.ClampLabelText(lblClanName, lblClanNameMaxWidth);
                }
                else
                    lblClanName.text = "";//Если нет клана - очищаем клановый текстмеш - не использем форматную строку, в которой могут быть например квадратные скобки
            }
            else//Если для имени клана используется текстмеш lblPlayerName
            {
                if(value.Length > 0)
                    PlayerName += string.Format(clanNameFormatString, clanNameColor.To2DToolKitColorFormatString(), value);
            }
        }
    }

    public int Score
	{
		set
		{
			if (!IsWorking)
				return;
			lblScore.text = value.ToString(GameData.instance.cultureInfo.NumberFormat);
		}
	}

	public int Kills
	{
		set
		{
			if (!IsWorking)
				return;
            lblKills.text = value.ToString(GameData.instance.cultureInfo.NumberFormat);
		}
	}

	public int Deaths
	{
		set
		{
			if (!IsWorking)
				return;
            lblDeaths.text = value.ToString(GameData.instance.cultureInfo.NumberFormat);
		}
	}

	public bool Premium
	{
		set
		{
			if (!IsWorking || sprPremiumIcon == null)
				return;
			sprPremiumIcon.gameObject.SetActive(value);
		}
	}

    public void AlignGoldRushIconIfNeeded()
    {
        if(useFlagGroupAlignment)
        {
            sprGoldRushLeader.transform.localPosition = flagGroup.sprKiller.transform.localPosition;
        }
    }

    public void SetupInterlacedBackgrounds(int num)
    {
        if (interlacedBackgrounds)
            interlacedBackgrounds.Activated = Mathf.Abs(num) % 2 == 0;
        //Debug.LogErrorFormat("string {0} must {1} the bg", num, Mathf.Abs(num) % 2 == 0 ? "activate" : "deactivate");
    }
}