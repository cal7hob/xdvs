using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "XD/Pattern", fileName = "CamoPattern_")]
public class PatternEntity : ScriptableObject
{
    [Serializable]
    public class PropertyKeyColorPair
    {
        public string propertyKey;
        public Color color;

        public PropertyKeyColorPair(string propertyKey)
        {
            this.propertyKey = propertyKey;
            this.color = Color.white;
        }

        public PropertyKeyColorPair(string propertyKey, Color color)
        {
            this.propertyKey = propertyKey;
            this.color = color;
        }
    }

    [Serializable]
    public class VehicleMaskScalePair
    {
        public int vehicleId;
        public Vector2 scale;
    }

    [NonSerialized]
    public ObscuredInt id;
    [NonSerialized]
    public ObscuredBool isHidden;
    [NonSerialized]
    public ObscuredBool isVip;
    [NonSerialized]
    public ObscuredInt availabilityLevel;
    [NonSerialized]
    public ObscuredInt position;
    [NonSerialized]
    public ObscuredDouble lifetime;
    [NonSerialized]
    public ObscuredFloat damageGain;
    [NonSerialized]
    public ObscuredFloat rocketDamageGain;
    [NonSerialized]
    public ObscuredFloat speedGain;
    [NonSerialized]
    public ObscuredFloat armorGain;
    [NonSerialized]
    public ObscuredFloat rofGain;
    [NonSerialized]
    public ObscuredFloat ircmRofGain;
    [NonSerialized]
    public List<ProfileInfo.Price> pricesToGroups;
    [NonSerialized]
    public bool isLoadedFromServer;

    [Header("Текстура")]
    public string maskPropertyKey = Pattern.MASK_TEX_KEY;
    public Texture textureMask;
    public string textureMaskPath;

    [Header("Основные цвета")]
    public List<PropertyKeyColorPair> colors = new List<PropertyKeyColorPair>
    {
        new PropertyKeyColorPair(Pattern.COLOR_FIRST_KEY),
        new PropertyKeyColorPair(Pattern.COLOR_SECOND_KEY),
        new PropertyKeyColorPair(Pattern.COLOR_THIRD_KEY)
    };

    [Header("Тайлинг (по умолчанию)")]
    public Vector2 scale = new Vector2(1, 1);

    [Header("Тайлинг (на ед. техники)")]
    public List<VehicleMaskScalePair> scales = new List<VehicleMaskScalePair>();

    public int ParsedId
    {
        get
        {
            return int.Parse(Regex.Match(name, @"(\d+)$").Groups[1].ToString());
        }
    }

    void OnEnable()
    {
        if (textureMask != null)
            textureMaskPath = SetTexturePath(textureMask);
        else if (!string.IsNullOrEmpty(textureMaskPath))
            textureMask = Resources.Load<Texture>(textureMaskPath);
    }

    public void SetTexture(Texture texture)
    {
        textureMask = texture;

        if (texture != null)
            textureMaskPath = SetTexturePath(texture);
    }

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
                .Select(price => (Dictionary<string, object>)price)
                .ToList();

        pricesToGroups = new List<ProfileInfo.Price>(list.Count);

        foreach (var curDict in list)
            pricesToGroups.Add(ProfileInfo.Price.FromDictionary(curDict));

        isLoadedFromServer = true;
    }

    public Texture LoadTextureMask()
    {
        Debug.LogWarning("PatternEntity.TextureMask() call! Use it just for editor.");
        return textureMask = textureMask ?? Resources.Load<Texture>(textureMaskPath);
    }

#if UNITY_EDITOR
    [ContextMenu("Insert RGB")]
    public void InsertRGB()
    {
        string colorsString = EditorGUIUtility.systemCopyBuffer;
        MatchCollection colorStringMatches = Regex.Matches(colorsString, "([a-fA-F0-9]{8})", RegexOptions.Singleline);
        string warning = "Input string {0} doesn't contain colors!";

        if (colorStringMatches.Count != 3)
        {
            Debug.LogWarning(string.Format(warning, colorsString));
            return;
        }

        string colorFirstString = "#" + colorStringMatches[0].Groups[1].ToString().Trim().TrimStart('#');
        string colorSecondString = "#" + colorStringMatches[1].Groups[1].ToString().Trim().TrimStart('#');
        string colorThirdString = "#" + colorStringMatches[2].Groups[1].ToString().Trim().TrimStart('#');

        Color colorFirst = new Color();
        Color colorSecond = new Color();
        Color colorThird = new Color();

        bool colorsParsed;

        colorsParsed = ColorUtility.TryParseHtmlString(colorFirstString, out colorFirst);
        colorsParsed = colorsParsed && ColorUtility.TryParseHtmlString(colorSecondString, out colorSecond);
        colorsParsed = colorsParsed && ColorUtility.TryParseHtmlString(colorThirdString, out colorThird);

        if (!colorsParsed)
        {
            Debug.LogWarning(string.Format(warning, colorsString));
            return;
        }

        colors = new List<PropertyKeyColorPair>
        {
            new PropertyKeyColorPair(Pattern.COLOR_FIRST_KEY, colorFirst),
            new PropertyKeyColorPair(Pattern.COLOR_SECOND_KEY, colorSecond),
            new PropertyKeyColorPair(Pattern.COLOR_THIRD_KEY, colorThird)
        };
    }
#endif

    private string SetTexturePath(Texture texture)
    {
        return string.Format("{0}/Camouflages/{1}", GameManager.CurrentResourcesFolder, texture.name);
    }
}
