using System;
using System.Linq;
using CodeStage.AntiCheat.ObscuredTypes;
using System.Collections.Generic;
using JSONObject = System.Collections.Generic.Dictionary<string, object>;

[Serializable]
public class VehicleInfo : HelpTools.IDictTransformable, IShopItem
{
    [Serializable]
    public enum TankType
    {
        Light,
        Medium,
        Heavy,
        Killer
    }

    [Serializable]
	public class ModuleUpgrade
	{
		public ObscuredFloat primaryGain;
		public ObscuredFloat secondaryGain;

        public JSONObject ToDictionary ()
        {
    		var dict = new JSONObject();

			dict["primaryGain"] = (float)primaryGain;
            dict["secondaryGain"] = (float)secondaryGain;

    		return dict;
		}

		public static ModuleUpgrade FromDictionary(JSONObject dict)
		{
			JsonPrefs data = new JsonPrefs(dict);
			ModuleUpgrade result = new ModuleUpgrade();

			result.primaryGain = data.ValueFloat("primaryGain");
			result.secondaryGain = data.ValueFloat("secondaryGain");

			return result;
		}
	}

    public int position;

	public ObscuredInt id;
	public ObscuredString vehicleName;
	public ObscuredInt vehicleGroup;
	public ObscuredInt availabilityLevel;
	public ObscuredBool isHidden = false;
    public ObscuredBool isVip = false;
    public ObscuredBool isIgnoringCritHits = false;
    public ObscuredBool isComingSoon = false;
    public TankType tankType;
    public ProfileInfo.Price price;
	
	public ObscuredFloat baseArmor;
	public ObscuredFloat baseDamage;
    public ObscuredFloat baseRocketDamage;
	public ObscuredFloat baseSpeed;
	public ObscuredFloat baseROF;
    public ObscuredFloat baseIRCMROF;

	public List<ModuleUpgrade> cannonUpgrades;
	public List<ModuleUpgrade> reloaderUpgrades;
	public List<ModuleUpgrade> armorUpgrades;
	public List<ModuleUpgrade> engineUpgrades;
	public List<ModuleUpgrade> tracksUpgrades;

    public bool IsPurchased { get { return ProfileInfo.vehicleUpgrades != null && ProfileInfo.vehicleUpgrades.ContainsKey(id); } }

    bool IShopItem.LockCondition
    {
        get { return ProfileInfo.Level < availabilityLevel && !IsPurchased; }
    }

    bool IShopItem.VipCondition
    {
        get { return isVip; }
    }

    bool IShopItem.HideCondition
    {
        get { return isHidden; }
    }

    bool IShopItem.ComingSoonCondition
    {
        get { return isComingSoon; }
    }

    int IShopItem.Id
    {
        get { return id; }
    }

    int IShopItem.AvailabilityLevel
    {
        get { return availabilityLevel; }
    }

    string IShopItem.Description
    {
        get { return vehicleName; }
    }

    public ProfileInfo.Price Price
    {
        get { return price; }
    }

    public int GetMaxUpgradeLevel(XD.ModuleType moduleType)
	{
		switch (moduleType)
		{
			case XD.ModuleType.Armor:
				return armorUpgrades.Count;
			case XD.ModuleType.Cannon:
				return cannonUpgrades.Count;
			case XD.ModuleType.Engine:
				return engineUpgrades.Count;
			case XD.ModuleType.Reloader:
				return reloaderUpgrades.Count;
			case XD.ModuleType.Tracks:
				return tracksUpgrades.Count;
			default:
				return 0;
		}
	}

	public void LoadFromDictionary(JSONObject dict)
	{
		JsonPrefs data = new JsonPrefs(dict);

        //Debug.Log("TankInfo.LoadFromDictionary -> " + data.ToString());

		id = data.ValueInt("id");
		position = data.ValueInt("position");
		vehicleName = data.ValueString("tankName");
		isHidden = data.ValueInt("hidden") > 0;
        isVip = data.ValueInt("vip") > 0;
        isComingSoon = data.ValueInt("comingSoon") > 0;
        isIgnoringCritHits = data.ValueInt("ignoringCritHits") > 0;
		vehicleGroup = data.ValueInt("tankGroup");
        availabilityLevel = data.ValueInt("availabilityLevel");

		price = ProfileInfo.Price.FromDictionary((JSONObject)dict["price"]);

		baseArmor = data.ValueInt("baseArmor");
		baseDamage = data.ValueInt("baseDamage");
        baseRocketDamage = data.ValueInt("baseRocketDamage");
		baseSpeed = data.ValueFloat("baseSpeed");
		baseROF = data.ValueFloat("baseROF");
        baseIRCMROF = data.ValueFloat("baseIrcmRof");

		FillModuleUpgrades(data, "cannonUpgrades", ref cannonUpgrades);
		FillModuleUpgrades(data, "reloaderUpgrades", ref reloaderUpgrades);
		FillModuleUpgrades(data, "armorUpgrades", ref armorUpgrades);
		FillModuleUpgrades(data, "engineUpgrades", ref engineUpgrades);
		FillModuleUpgrades(data, "tracksUpgrades", ref tracksUpgrades);

	    dict["cannonUpgrades"] = cannonUpgrades.Select(item => (object)item.ToDictionary()).ToList();
	    dict["reloaderUpgrades"] = reloaderUpgrades.Select(item => (object)item.ToDictionary()).ToList();
	    dict["armorUpgrades"] = armorUpgrades.Select(item => (object)item.ToDictionary()).ToList();
	    dict["engineUpgrades"] = engineUpgrades.Select(item => (object)item.ToDictionary()).ToList();
	    dict["tracksUpgrades"] = tracksUpgrades.Select(item => (object)item.ToDictionary()).ToList();
	}

    public JSONObject ToDictionary()
    {
    	var dict = new JSONObject(18);

        dict["id"] = id;
        dict["position"] = position;
        dict["ignoringCritHits"] = Convert.ToInt32(isIgnoringCritHits);
        dict["tankName"] = vehicleName;
        dict["tankGroup"] = vehicleGroup;
        dict["availabilityLevel"] = availabilityLevel;
        dict["price"] = price.ToDictionary();
        dict["baseArmor"] = (int)baseArmor;
        dict["baseDamage"] = (int)baseDamage;
        dict["baserocketDamage"] = (int)baseRocketDamage;
        dict["baseSpeed"] = (float)baseSpeed;
        dict["baseROF"] = (float)baseROF;
        dict["baseIrcmRof"] = (float)baseIRCMROF;
        dict["cannonUpgrades"] = cannonUpgrades.Select(item => (object)item.ToDictionary()).ToList();
        dict["reloaderUpgrades"] = reloaderUpgrades.Select(item => (object)item.ToDictionary()).ToList();
        dict["armorUpgrades"] = armorUpgrades.Select(item => (object)item.ToDictionary()).ToList();
        dict["engineUpgrades"] = engineUpgrades.Select(item => (object)item.ToDictionary()).ToList();
        dict["tracksUpgrades"] = tracksUpgrades.Select(item => (object)item.ToDictionary()).ToList();
    	
    	return dict;
    }

	private void FillModuleUpgrades(JsonPrefs jsonData, string key, ref List<ModuleUpgrade> outList)
	{
        //Debug.Log("VehicleInfo.FillModuleUpgrades " + key + " -> " + jsonData.ToString());
		List<object> data = jsonData.ValueObjectList(key);
		outList = data.Select(x => ModuleUpgrade.FromDictionary((JSONObject)x)).ToList();
	}
}