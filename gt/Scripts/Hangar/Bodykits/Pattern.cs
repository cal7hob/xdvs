using System.Collections.Generic;
using UnityEngine;

public class Pattern : Bodykit
{
    /// <summary>
    /// Название шейдерной проперти для текстуры камуфляжа.
    /// </summary>
    public string maskPropertyKey;
    public Texture textureMask;
    public Vector2 scale;

    private readonly string texturePath;

    public Pattern(PatternInEditor sourcePattern) : base(sourcePattern)
    {
        maskPropertyKey = sourcePattern.maskPropertyKey;
        scale = sourcePattern.scale;

        PropertyKeysToColors = new Dictionary<string, Color>();
        TanksToMaskScales = new Dictionary<Constants.TankAR, Vector2>();

        foreach (PatternInEditor.PropertyKeyColorPair propertyKeyColorPair in sourcePattern.colors)
            PropertyKeysToColors.Add(propertyKeyColorPair.propertyKey, propertyKeyColorPair.color);

        foreach (PatternInEditor.TankMaskScalePair tankMaskScalePair in sourcePattern.scales)
            TanksToMaskScales.Add(tankMaskScalePair.tank, tankMaskScalePair.scale);

        if (sourcePattern.textureMask == null)
            Debug.LogErrorFormat("Texture mask of pattern {0} is NULL!", sourcePattern);
        else
            texturePath = string.Format("{0}/Camouflages/{1}", GameManager.CurrentResourcesFolder, sourcePattern.textureMask.name);
    }

    /// <summary>
    /// Название шейдерных пропертей для цветов маски (текстуры камуфляжа).
    /// </summary>
    public Dictionary<string, Color> PropertyKeysToColors { get; private set; }

    public Dictionary<Constants.TankAR, Vector2> TanksToMaskScales { get; private set; }

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
                    GameManager.CurrentResourcesFolder);

            return textureMask;
        }
    }

    public Vector2 GetScale(int tankId)
    {
        if (GameData.IsGame(Game.WWT2))
        {
            Vector2 result;

            if (TanksToMaskScales.TryGetValue((Constants.TankAR)tankId, out result))
                return result;
        }

        return scale;
    }
}
