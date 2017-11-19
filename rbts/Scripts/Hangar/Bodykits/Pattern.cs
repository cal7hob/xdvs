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
        scale = sourcePattern.defaultScale;

        PropertyKeysToColors = new Dictionary<string, Color>();
        TanksToMaskScales = new Dictionary<int, Vector2>();

        foreach (PatternInEditor.PropertyKeyColorPair propertyKeyColorPair in sourcePattern.colors)
            PropertyKeysToColors.Add(propertyKeyColorPair.propertyKey, propertyKeyColorPair.color);

        foreach (PatternInEditor.VehicleMaskScalePair tankMaskScalePair in sourcePattern.scales)
			TanksToMaskScales.Add(tankMaskScalePair.vehicleId, tankMaskScalePair.scale);

        if (string.IsNullOrEmpty(sourcePattern.textureMaskName))
            Debug.LogErrorFormat("Texture mask of pattern {0} is not defined!", sourcePattern);
        else
            texturePath = string.Format("{0}/Camouflages/{1}", GameManager.CurrentResourcesFolder,
                sourcePattern.textureMaskName);

        sourcePattern.textureMaskName = null;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    public void Dispose()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    /// <summary>
    /// Название шейдерных пропертей для цветов маски (текстуры камуфляжа).
    /// </summary>
    public Dictionary<string, Color> PropertyKeysToColors { get; private set; }

    public Dictionary<int, Vector2> TanksToMaskScales { get; private set; }

    /// <summary>
    /// Текстура камуфляжа.
    /// </summary>
    public Texture TextureMask
    {
        get
        {
            if (textureMask == null)
            {
                textureMask = Resources.Load<Texture>(texturePath);
                if (textureMask == null)
                {
                    Debug.LogErrorFormat(
                        "Camouflage #{0} wasn't defined or located in wrong directory instead of \"/Assets/Resources/{1}/Camouflages\"!",
                        id,
                        GameManager.CurrentResourcesFolder);
                }
            }

            return textureMask;
        }
    }

    public Vector2 GetScale(int vehicleId)
    {
		Vector2 result;

		if (TanksToMaskScales.TryGetValue(vehicleId, out result))
			return result;

        return scale;
    }

    private void OnSceneUnloaded(Scene scene)
    {
        Resources.UnloadAsset(textureMask);
        textureMask = null;
    }
}
