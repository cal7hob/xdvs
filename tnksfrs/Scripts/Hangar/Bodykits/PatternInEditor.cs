using XD;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;

public class PatternInEditor : BodykitInEditor, ICamouflage
{
    public Vector2                      scale = new Vector2(1, 1);
    public string                       maskPropertyKey = "";
    public Texture                      textureMask = null;
    
    [Header("Основные цвета")]
    public List<PropertyKeyColorPair>   colors = null;
    [Header("Боевые цвета (только для вертолётов)")]
    public List<PropertyKeyColorPair>   battleColors = null; // Очень костыльно, не надо так.

    public Dictionary<string, Color> PropertyKeysToColors
    {
        get; private set;
    }

    public List<PropertyKeyColorPair> Colors
    {
        get
        {
            return colors;
        }
    }

    public CurrencyValue CurrentPrice
    {
        get;
        set;
    }

    /*public List<PropertyKeyColorPair> BattleColors
    {
        get
        {
            return battleColors;
        }
    }*/

    public Vector2 Tiling
    {
        get
        {
            return scale;
        }
    }

    public string MaskPropertyKey
    {
        get
        {
            return maskPropertyKey;
        }
    }

    public Texture TextureMask
    {
        get
        {
            return textureMask;
        }
    }

    public FXLocation CurrentType
    {
        get; set;
    }

    public bool IsNational
    {
        get;
    }

    public Dictionary<string, Color> GetColorSet(FXLocation location)
    {
        throw new System.NotImplementedException();
    }

    public int ID
    {
        get
        {
            return id;
        }
    }

    public bool IsHidden
    {
        get
        {
            return isHidden;
        }
    }

    public bool IsVip
    {
        get
        {
            return isVip;
        }
    }
    
    public ObscuredInt AvailabilityLevel
    {
        get
        {
            return availabilityLevel;
        }
    }
    
    public ObscuredFloat DamageGain
    {
        get
        {
            return damageGain;
        }
    }

    public ObscuredFloat RocketDamageGain
    {
        get
        {
            return rocketDamageGain;
        }
    }

    public ObscuredFloat SpeedGain
    {
        get
        {
            return speedGain;
        }
    }

    public ObscuredFloat ArmorGain
    {
        get
        {
            return armorGain;
        }
    }

    public ObscuredFloat RofGain
    {
        get
        {
            return rofGain;
        }
    }

    public ObscuredFloat IrcmRofGain
    {
        get
        {
            return ircmRofGain;
        }
    }

    public List<ProfileInfo.Price> PricesToGroups
    {
        get
        {
            return pricesToGroups;
        }
    }
}
