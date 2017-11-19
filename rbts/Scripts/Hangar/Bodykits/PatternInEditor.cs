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
    public class VehicleMaskScalePair
    {
        public int vehicleId;
        public Vector2 scale;
    }

    public string maskPropertyKey;
    public string textureMaskName;
    
    [Header("Основные цвета")]
    public List<PropertyKeyColorPair> colors;

	[Header("Скейл маски (по умолчанию)")]
	[Tooltip("Применяется, если не нашёлся подходящий, среди scales.")]
	public Vector2 defaultScale = new Vector2(1, 1);

	[Space]
	[Header("Скейлы маски (на для ед. техники)")]
    public List<VehicleMaskScalePair> scales;
}
