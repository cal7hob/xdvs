using System;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;
using XD;

public class Pattern : Bodykit, ICamouflage
{
    /// <summary>
    /// Название шейдерной проперти для текстуры камуфляжа.
    /// </summary>
    public string               maskPropertyKey;
    public Texture              textureMask;
    public Vector2              tiling;
    private readonly string     texturePath = "";

    public Pattern(ICamouflage sourcePattern) : base(sourcePattern)
    {
        maskPropertyKey = sourcePattern.MaskPropertyKey;
        tiling = sourcePattern.Tiling;

        PropertyKeysToColors = new Dictionary<string, Color>();
        PropertyKeysToBattleColors = new Dictionary<string, Color>();

        foreach (PropertyKeyColorPair propertyKeyColorPair in sourcePattern.Colors)
        {
            PropertyKeysToColors.Add(propertyKeyColorPair.propertyKey, propertyKeyColorPair.color);
        }
        
        if (sourcePattern.TextureMask == null)
        {
            Debug.LogErrorFormat("Texture mask of pattern {0} is NULL!", sourcePattern);
        }
        else
        {
            texturePath = string.Format("{0}/Camouflages/{1}", StaticContainer.GameManager.CurrentResourcesFolder, sourcePattern.TextureMask.name);
        }
    }

    /// <summary>
    /// Название шейдерных пропертей для цветов маски (текстуры камуфляжа).
    /// </summary>
    public Dictionary<string, Color> PropertyKeysToColors { get; private set; }

    /// <summary>
    /// Название шейдерных пропертей для цветов маски (текстуры камуфляжа).
    /// Костыль с отдельными цветами для вертолётов в бою.
    /// </summary>
    public Dictionary<string, Color> PropertyKeysToBattleColors { get; private set; }

    /// <summary>
    /// Текстура камуфляжа.
    /// </summary>
    public Texture TextureMask
    {
        get
        {
            textureMask = textureMask ?? Resources.Load<Texture>(texturePath);
            
            if(textureMask == null)
                Debug.LogErrorFormat(
                    "Camouflage #{0} wasn't defined or located in wrong directory instead of \"/Assets/Resources/{1}/Camouflages\"!",
                    id,
                    StaticContainer.GameManager.CurrentResourcesFolder);

            return textureMask;
        }
    }

    public FXLocation CurrentType
    {
        get; 
        set;
    }

    public bool IsNational
    {
        get;
    }

    public Dictionary<string, Color> GetColorSet(FXLocation location)
    {
        throw new NotImplementedException();
    }

    public List<PropertyKeyColorPair> Colors
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public CurrencyValue CurrentPrice
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public Vector2 Tiling
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public string MaskPropertyKey
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public int ID
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public bool IsHidden
    {
        get
        {
            return false;
        }
    }

    public bool IsVip
    {
        get
        {
            return false;
        }
    }

    public ObscuredInt AvailabilityLevel
    {
        get
        {
            throw new NotImplementedException();
        }
    }
    
    public ObscuredFloat DamageGain
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public ObscuredFloat RocketDamageGain
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public ObscuredFloat SpeedGain
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public ObscuredFloat ArmorGain
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public ObscuredFloat RofGain
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public ObscuredFloat IrcmRofGain
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public List<ProfileInfo.Price> PricesToGroups
    {
        get
        {
            throw new NotImplementedException();
        }
    }
}
