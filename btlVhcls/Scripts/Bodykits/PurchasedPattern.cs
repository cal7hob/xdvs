using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class PurchasedPattern
{
    public int id;

    private double lifetime;
    private double deathTime;

    public PurchasedPattern(int camoId, double camoLifetime)
    {
        id = camoId;
        lifetime = camoLifetime;
        deathTime = GameData.CurrentTimeStamp + lifetime * 60 * 60;
    }

    public double Lifetime { get { return lifetime; } }
    public double Deathtime { get { return deathTime; } }

    public bool IsDead()
    {
        return deathTime < GameData.CurrentTimeStamp;
    }

    public Dictionary<string, object> ToDictionary()
    {
    	Dictionary<string, object> dict = new Dictionary<string, object>();
    	
    	dict["id"] = id;
    	dict["lifetime"] = lifetime;
        dict["deathTime"] = deathTime;
    	
    	return dict;
    }

    public static TPurchasedBodykit FromDictionary<TPurchasedBodykit>(Dictionary<string, object> dict)
        where TPurchasedBodykit : PurchasedPattern
    {
        JsonPrefs jsonPrefs = new JsonPrefs(dict);
    	
        TPurchasedBodykit purchasedBodykit = (TPurchasedBodykit)Activator.CreateInstance(
            typeof(TPurchasedBodykit),
            jsonPrefs.ValueInt("id"),
            jsonPrefs.ValueDouble("lifetime"));

        purchasedBodykit.deathTime = jsonPrefs.ValueDouble("deathTime");

    	return purchasedBodykit;
    }
}

public static class PurchasedPatternExtension
{
    public static bool TryToTakeAwayById(this List<PurchasedPattern> source, int camoId, out int nextCamoId)
    {
        PurchasedPattern targetPattern = source.FirstOrDefault(camo => camo.id == camoId) ?? source.First();

        if (targetPattern.IsDead())
        {
            source.Remove(targetPattern);

            for (int i = source.Count - 1; i >= 0; --i) {
                if (source[i].IsDead()) {
                    source.RemoveAt(i);
                }
            }

            nextCamoId = source.Any() ? source.First().id : 0;

            return true;
        }

        nextCamoId = targetPattern.id;

        return false;
    }
}
