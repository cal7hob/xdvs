using System;
using System.Collections.Generic;
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
    
    [Header("Основные цвета")]
    public List<PropertyKeyColorPair> colors;

    [Header("Скейлы маски (только для WWT2)")]
    public List<TankMaskScalePair> scales; // Очень костыльно, не надо так.
}
