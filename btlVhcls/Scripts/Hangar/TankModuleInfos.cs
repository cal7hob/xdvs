using System;
using System.Collections.Generic;
using System.Linq;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;

using JSONObject = System.Collections.Generic.Dictionary<string, object>;

#if !UNITY_WSA
using System.Security.Cryptography.X509Certificates;
#endif

public static class TankModules
{
    public static TankModuleInfos.Module cannon;
    public static TankModuleInfos.Module reloader;
    public static TankModuleInfos.Module armor;
    public static TankModuleInfos.Module engine;
    public static TankModuleInfos.Module tracks;
}

public class TankModuleInfos : MonoBehaviour, HelpTools.IDictTransformable
{
	public enum ModuleType
	{
		None,
		Cannon,
		Reloader,
		Armor,
		Engine,
		Tracks
	}
	
	[Serializable]
	public class Module
	{
		public ModuleType type;
		public VehicleInfo.VehicleParameter primaryParameter;
		public VehicleInfo.VehicleParameter secondaryParameter;
		public ObscuredFloat priceRatio;
		public ObscuredFloat[] prices;

		public int GetPrice(int level) { return (int)(prices[level - 1] * priceRatio); }

        public JSONObject ToDictionary()
		{
			JSONObject dict = new JSONObject
			{
				{ "type", type },
				{ "primaryParameter", primaryParameter },
				{ "secondaryParameter", secondaryParameter },
				{ "priceRatio", priceRatio.ToString("F2") },
				{ "prices", prices.Select(x=>(float)x).ToList() }
			};

			return dict;
		}

		public static Module FromDictionary(JSONObject dict)
		{
			Module info = new Module();
			info.type = (ModuleType)Enum.Parse(typeof(ModuleType), (string)dict["type"]);
			info.primaryParameter = (VehicleInfo.VehicleParameter)Enum.Parse(typeof(VehicleInfo.VehicleParameter), (string)dict["primaryParameter"]);
			info.secondaryParameter = (VehicleInfo.VehicleParameter)Enum.Parse(typeof(VehicleInfo.VehicleParameter), (string)dict["secondaryParameter"]);
			JsonPrefs data = new JsonPrefs(dict);
			info.priceRatio = data.ValueFloat("priceRatio");
			List<object> list = data.ValueObjectList("prices");
			info.prices = new ObscuredFloat [list.Count];
            int i = 0;
            foreach (var val in list) {
                info.prices[i] = Convert.ToSingle (val);
                i++;
            }

			return info;
		}
	}
	
	public ObscuredFloat[] speedupPrices;
	public ObscuredFloat[] inventionTime; // Invention time in minutes

    public Module cannon;
    public Module reloader;
    public Module armor;
    public Module engine;
    public Module tracks;

    public static TankModuleInfos Instance
	{
		get { return instance; }
	}

	private static TankModuleInfos instance;

	void Awake()
	{
		instance = this;
	}

	void OnDestroy()
	{
		instance = null;
	}

	public static int GetQuickDeliveryPrice(float minutes)
	{
		var speedupPrices = instance.speedupPrices;
		var inventionTime = instance.inventionTime;
		for (int i = 0; i < inventionTime.Length - 1; i++)
		{
			if (minutes <= inventionTime[i])
				return Mathf.Clamp(Mathf.CeilToInt(speedupPrices[i] / inventionTime[i] * minutes), 1, int.MaxValue);
		}

		return Mathf.RoundToInt(speedupPrices[inventionTime.Length - 1] / inventionTime[inventionTime.Length - 1] * minutes);
	}

    public JSONObject ToDictionary()
	{
		JSONObject dict = new JSONObject
		{
			{"speedupPrices", speedupPrices.Select(x=>(float)x).ToList()},
			{"inventionTime", inventionTime.Select(x=>(float)x).ToList()},
			{"cannonInfo", TankModules.cannon.ToDictionary()},
			{"reloaderInfo", TankModules.reloader.ToDictionary()},
			{"armorInfo", TankModules.armor.ToDictionary()},
			{"engineInfo", TankModules.engine.ToDictionary()},
			{"tracksInfo", TankModules.tracks.ToDictionary()}
		};

		return dict;
	}

	public void LoadFromDictionary(JSONObject dict)
	{
		JsonPrefs data = new JsonPrefs(dict);
		List<object> list = data.ValueObjectList("speedupPrices");
		speedupPrices = new ObscuredFloat[list.Count];

        int i = 0;

        foreach (var val in list) {
            speedupPrices[i] = Convert.ToSingle (val);
            i++;
        }

		list = data.ValueObjectList("inventionTime");

		inventionTime = new ObscuredFloat[list.Count];
        i = 0;

        foreach (var val in list) {
            inventionTime[i] = Convert.ToSingle (val);
            i++;
        }

        cannon = TankModules.cannon = Module.FromDictionary(data.ValueObjectDict("cannonInfo"));
        armor = TankModules.armor = Module.FromDictionary(data.ValueObjectDict("armorInfo"));
        engine = TankModules.engine = Module.FromDictionary(data.ValueObjectDict("engineInfo"));
        tracks = TankModules.tracks = Module.FromDictionary(data.ValueObjectDict("tracksInfo"));
        reloader = TankModules.reloader = Module.FromDictionary(data.ValueObjectDict("reloaderInfo"));
    }
}
