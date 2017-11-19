using System.Collections.Generic;
using System.Linq;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;

public class BodykitInEditor : MonoBehaviour, HelpTools.IDictTransformable
{
	public ObscuredInt  id;
    public ObscuredBool isHidden;
    public ObscuredBool isVip;
    public ObscuredInt  availabilityLevel;
    public ObscuredInt  position;
	public ObscuredDouble lifetime;
    public ObscuredFloat damageGain;
    public ObscuredFloat rocketDamageGain;
	public ObscuredFloat speedGain;
	public ObscuredFloat armorGain;
	public ObscuredFloat rofGain;
    public ObscuredFloat ircmRofGain;
    public List<ProfileInfo.Price> pricesToGroups;

	public Dictionary<string, object> ToDictionary()
	{
		Dictionary<string, object>[] prices = new Dictionary<string, object>[pricesToGroups.Count];

		for (int i = 0; i < pricesToGroups.Count; i++)
            prices[i] = pricesToGroups[i].ToDictionary();
		
		Dictionary<string, object> dict = new Dictionary<string, object>
		{
			{ "id", id },
            { "hidden", isHidden },
            { "vip", isVip },
            { "availabilityLevel", availabilityLevel },
            { "position", position },
			{ "lifetime", lifetime },
			{ "damageGain", damageGain },
            { "rocketDamageGain", rocketDamageGain },
			{ "armorGain", armorGain },
			{ "rofGain", rofGain },
            { "ircmRofGain", ircmRofGain },
			{ "speedGain", speedGain },
			{ "pricesToGroups", prices }
		};

		return dict;
	}

	public void LoadFromDictionary(Dictionary<string, object> dict)
	{
		JsonPrefs data = new JsonPrefs(dict);

		id = data.ValueInt("id");
        isHidden = data.ValueBool("hidden");
        isVip = data.ValueBool("vip");
        availabilityLevel = data.ValueInt("availabilityLevel");
        position = data.ValueInt("position");
		lifetime = data.ValueDouble("lifetime");
		damageGain = data.ValueFloat("damageGain");
        rocketDamageGain = data.ValueFloat("rocketDamageGain");
		armorGain = data.ValueFloat("armorGain");
		speedGain = data.ValueFloat("speedGain");
		rofGain = data.ValueFloat("rofGain");
        ircmRofGain = data.ValueFloat("ircmRofGain");

		List<Dictionary<string, object>> list
            = data.ValueObjectList("pricesToGroups")
                .Select(price => (Dictionary<string, object>) price)
                .ToList();

		pricesToGroups = new List<ProfileInfo.Price>(list.Count);

		foreach (var curDict in list)
			pricesToGroups.Add(ProfileInfo.Price.FromDictionary(curDict));
	}
}