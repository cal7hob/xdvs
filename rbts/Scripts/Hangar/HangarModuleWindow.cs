using System;
using UnityEngine;

public class HangarModuleWindow : MonoBehaviour
{
    public static event Action<TankModuleInfos.ModuleType, int> OnLevelChange = delegate {};

    [SerializeField]
    private tk2dSprite sprPicture;

    [SerializeField]
    private tk2dTextMesh lblTitle;

    [SerializeField]
    private tk2dTextMesh lblDescription;

    [SerializeField]
    private tk2dTextMesh lblLevel;

    [SerializeField]
    private GameObject leftButton;

    [SerializeField]
    private GameObject rightButton;

    private static HangarModuleWindow instance;

    private int level = 1;
    private int maxLevel = 1;
	private LabelLocalizationAgent levelLabelAgent;
	private TankModuleInfos.ModuleType type;
    private VehicleInfo.VehicleType vehType;
	
    void Awake()
    {
        instance = this;

        if (sprPicture == null && (GameData.IsGame(Game.IronTanks | Game.SpaceJet | Game.BattleOfWarplanes)))
            sprPicture = GetComponentInChild<tk2dSprite>("sprPicture");

        if(lblTitle == null && !GameData.IsGame(Game.SpaceJet | Game.BattleOfWarplanes | Game.BattleOfHelicopters))// В SpaceJet лейбл описания не используется.
            lblTitle = GetComponentInChild<tk2dTextMesh>("lblTitle");

        if (lblDescription == null && !GameData.IsGame(Game.SpaceJet | Game.BattleOfWarplanes | Game.BattleOfHelicopters)) // В SpaceJet лейбл описания не используется.
            lblDescription = GetComponentInChild<tk2dTextMesh>("lblDescription");

        if (lblLevel == null)
            lblLevel = GetComponentInChild<tk2dTextMesh>("lblLevel");

        levelLabelAgent = lblLevel.GetComponent<LabelLocalizationAgent>();

        if (leftButton == null)
            leftButton = transform.Find("lblModuleLevelPrev").gameObject;

        if (rightButton == null)
            rightButton = transform.Find("lblModuleLevelNext").gameObject;
    }

    void Start()
    {
        GUIController.ListenButtonClick("lblModuleLevelPrev", ChangeLevel);
        GUIController.ListenButtonClick("lblModuleLevelNext", ChangeLevel);
    }

    void OnDestroy() { instance = null; }

    public static void SetData(TankModuleInfos.ModuleType type, int level, int maxLevel, VehicleInfo.VehicleType vehicleType)
	{
		if (maxLevel < 1)
			maxLevel = 1;

		level = Mathf.Clamp(level, 1, maxLevel);

		instance.type = type;
		instance.level = level;
        instance.maxLevel = maxLevel;
        instance.vehType = vehicleType;
		instance.SetLevelText();

		OnLevelChange(instance.type, instance.level);
	}

	public static int Level
	{
		get { return instance.level; }
	}

	/*	PRIVATE SECTION */
	private T GetComponentInChild<T>(string childName)
		where T: MonoBehaviour
	{
		Transform trans = transform.Find(childName);
		if (!trans)
		{
			DT.LogError("There is no child '{0}'", childName);
			return null;
		}

		T comp = trans.GetComponent<T>();
		if (!comp)
		{
			DT.LogError("There is no component '{0}' in child '{1}'", typeof(T).Name, childName);
			return null;
		}

		return comp;
	}

	private void ChangeLevel(string buttonName)
	{
		int _level = level;
		switch (buttonName)
		{
			case "lblModuleLevelPrev":
				_level--;
				break;
			case "lblModuleLevelNext":
				_level++;
				break;
		}

		_level = Mathf.Clamp(_level, 1, maxLevel);

		if (level == _level)
			return;

		level = _level;
		levelLabelAgent.Parameter = level.ToString();
		SetLevelText();
		OnLevelChange(type, level);
	}

	private void SetLevelText()
	{
        int classLevel = 1 + (level - 1) / 5;
		levelLabelAgent.Parameter = level.ToString();
        string moduleName = string.Format("Module_{0}{1}", type, classLevel);

        string nameKey = string.Format("{0}_Name", moduleName);
        string descriptionKey = string.Format("{0}_Description", moduleName);

        if (lblTitle)
            lblTitle.text = GetVehDependentText(nameKey);
        if (lblDescription)
            lblDescription.text = GetVehDependentText(descriptionKey);

		//Set level one picture for the time being..
        if (GameData.IsGame(Game.IronTanks))
			sprPicture.SetSprite(moduleName.Substring(0, moduleName.Length - 1) + classLevel);
        else if(GameData.IsGame(Game.SpaceJet | Game.BattleOfWarplanes | Game.Armada))
            sprPicture.SetSprite(moduleName.Substring(0, moduleName.Length - 1));
        else if(GameData.IsGame(Game.BattleOfHelicopters | Game.Armada))
            sprPicture.SetSprite("armory_" + type.ToString().ToLower());
        leftButton.SetActive(level > 1);
		rightButton.SetActive(level < maxLevel);
	}

    private string GetVehDependentText(string locKey)
    {
        string text = Localizer.GetTextSilently(string.Format("{0}_{1}", locKey, vehType));
        if (string.IsNullOrEmpty(text))
        {
            text = Localizer.GetText(locKey);
        }

        return text;
    }
}
