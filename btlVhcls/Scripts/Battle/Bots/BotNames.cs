using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class BotName
{
    public string Name { get; private set; }
    public string Country { get; private set; }

    public BotName(string name, string country)
    {
        Name = name;
        Country = country;
    }
}

public class BotNames
{
    private List<BotName> names = new List<BotName>();
    /// <summary>
    /// Creates BotNames instance from json.
    /// </summary>
    /// <param name="json">JsonPrefs with group contained "bots" subgroup</param>
    public BotNames(JsonPrefs json)
    {
        if (!json.Contains("bots"))
        {
            Debug.LogError("There is no 'bots' section in parsed data");
            return;
        }
        json.BeginGroup("bots");
        if (!json.Contains("names"))
        {
            Debug.LogError("There is no 'names' section in parsed data");
            return;
        }
        List<object> serialized = json.ValueObjectList("names", null);
        json.EndGroup();
        if (serialized == null)
        {
            Debug.LogError("Invalid bot names format!");
            return;
        }
        foreach (object obj in serialized)
        {
            string name, country;
            Dictionary<string, object> botName = (Dictionary<string, object>) obj;
            if (!botName.ContainsKey("name") || !botName.ContainsKey("countryCode"))
            {
                Debug.LogError("Error while parsing separate bot name");
                return;
            }
            name = (string) botName["name"];
            country = (string) botName["countryCode"];
            if (name == null || country == null)
            {
                Debug.LogError("Error while parsing separate bot name");
                return;
            }

            names.Add(new BotName(name, country.ToLower()));
        }
        names.Shuffle();
    }

    public BotNames(List<BotName> botNames)
    {
        names = botNames;
    }

    public static BotNames GetDummyNames()
    {
        int maxBotCount = ProfileInfo.IsBattleTutorial ? 4 : GameData.maxPlayers;
        string locTarget = Localizer.GetText("tutorialBotName");
        List<BotName> botNamesList = new List<BotName>();

        for (int i = 1; i <= maxBotCount; i++)
            botNamesList.Add(new BotName(string.Format("{0} {1}", locTarget, i), "xx"));

        return new BotNames(botNamesList);
    }

    public BotName GetName(bool uniqueForBattle)
    {
        if (names.Count == 0)
            return null;

        BotName botName;

        do
        {
            botName = names[0];
            names.Remove(botName);
            names.Insert(names.Count - 1, botName);
        }
        while (uniqueForBattle && !CheckBotNameForUnique(botName.Name));

        return botName;
    }

    private bool CheckBotNameForUnique(string botName)
    {
        foreach (VehicleController veh in BattleController.allVehicles.Values)
        {
            if (veh.data.playerName == botName)
                return false;
        }
        return true;
    }
}