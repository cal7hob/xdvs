using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        PropertyKeysToBattleColors = new Dictionary<string, Color>();
        TanksToMaskScales = new Dictionary<int, Vector2>();

        foreach (PatternInEditor.PropertyKeyColorPair propertyKeyColorPair in sourcePattern.colors)
            PropertyKeysToColors.Add(propertyKeyColorPair.propertyKey, propertyKeyColorPair.color);

        foreach (PatternInEditor.PropertyKeyColorPair propertyKeyColorPair in sourcePattern.battleColors)
            PropertyKeysToBattleColors.Add(propertyKeyColorPair.propertyKey, propertyKeyColorPair.color);

        foreach (PatternInEditor.TankMaskScalePair tankMaskScalePair in sourcePattern.scales)
            TanksToMaskScales.Add((int)tankMaskScalePair.tank, tankMaskScalePair.scale);

        if (sourcePattern.textureMask == null)
            Debug.LogErrorFormat("Texture mask of pattern {0} is NULL!", sourcePattern);
        else
            texturePath = string.Format("{0}/Camouflages/{1}", GameManager.CurrentResourcesFolder, sourcePattern.textureMask.name);

        if (!Application.isPlaying)
            textureMask = sourcePattern.textureMask;

        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    public Pattern(PatternEntity patternEntity) : base(patternEntity)
    {
        maskPropertyKey = patternEntity.maskPropertyKey;
        scale = patternEntity.scale;

        PropertyKeysToColors = new Dictionary<string, Color>();
        PropertyKeysToBattleColors = new Dictionary<string, Color>();
        TanksToMaskScales = new Dictionary<int, Vector2>();

        foreach (PatternEntity.PropertyKeyColorPair propertyKeyColorPair in patternEntity.colors)
            PropertyKeysToColors.Add(propertyKeyColorPair.propertyKey, propertyKeyColorPair.color);

        foreach (PatternEntity.VehicleMaskScalePair vehicleMaskScalePair in patternEntity.scales)
            TanksToMaskScales[vehicleMaskScalePair.vehicleId] = vehicleMaskScalePair.scale;

        if (patternEntity.textureMask != null)
            texturePath = string.Format("{0}/Camouflages/{1}", GameManager.CurrentResourcesFolder, patternEntity.textureMask.name);
        else
            texturePath = patternEntity.textureMaskPath;

        if (texturePath.Length == 0)
            Debug.LogErrorFormat("Texture mask of pattern {0} not found!", patternEntity.name);

        Resources.UnloadAsset(patternEntity.textureMask);
        patternEntity.textureMask = null;

        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    ~Pattern()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
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

    public Dictionary<int, Vector2> TanksToMaskScales { get; private set; }

    /// <summary>
    /// Текстура камуфляжа.
    /// </summary>
    public Texture TextureMask
    {
        get
        {
            textureMask = textureMask ?? Resources.Load<Texture>(texturePath);
            
            if (textureMask == null)
                Debug.LogErrorFormat(
                    "Camouflage #{0} wasn't defined or located in wrong directory instead of \"/Assets/Resources/{1}/Camouflages\"!",
                    id,
                    GameManager.CurrentResourcesFolder);

            return textureMask;
        }
    }

    public Texture GetTextureMask(string camoTextureDirectory)
    {
        if (textureMask != null)
            return textureMask;

        string[] texturePathParts = texturePath.Split('/');
        string textureName = texturePathParts[texturePathParts.Length - 1];

       return Resources.Load<Texture>(camoTextureDirectory + textureName);
    }

    public Vector2 GetScale(int vehicleId)
    {
        Vector2 result;

        if (TanksToMaskScales.TryGetValue(vehicleId, out result))
            return result;

        return scale;
    }

    public void UnloadTexture()
    {
        Resources.UnloadAsset(textureMask);
        textureMask = null;
        GC.Collect();
    }

    private void OnSceneUnloaded(Scene scene)
    {
        UnloadTexture();
    }

    public const string MASK_TEX_KEY = "_MaskTex";
    public const string COLOR_FIRST_KEY = "_Color1";
    public const string COLOR_SECOND_KEY = "_Color2";
    public const string COLOR_THIRD_KEY = "_Color3";

    public PurchasedPattern ToPurchased()
    {
        return new PurchasedPattern(id, lifetime);
    }
}
