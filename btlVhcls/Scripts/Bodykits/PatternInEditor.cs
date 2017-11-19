using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class PatternInEditor : BodykitInEditor
{
    [Serializable]
    public class PropertyKeyColorPair
    {
        public string propertyKey;
        public Color color;
    }

    [Serializable]
    public class TankMaskScalePair
    {
        public Constants.TankAR tank;
        public Vector2 scale;
    }

    public Vector2 scale = new Vector2(1, 1);
    public string maskPropertyKey;
    public Texture textureMask;

    public int ParsedId
    {
        get
        {
            return int.Parse(Regex.Match(name, @"(\d+)$").Groups[1].ToString());
        }
    }

    [Header("Основные цвета")]
    public List<PropertyKeyColorPair> colors;

    [Header("Боевые цвета (только для вертолётов)")]
    public List<PropertyKeyColorPair> battleColors; // Очень костыльно, не надо так.

    [Header("Скейлы маски (только для Armada)")]
    public List<TankMaskScalePair> scales; // Очень костыльно, не надо так.

    public void UnloadTexture()
    {
        Resources.UnloadAsset(textureMask);
        textureMask = null;
    }
}
