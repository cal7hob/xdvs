using UnityEngine;

public class RewiredCustomButtonAttribute : PropertyAttribute
{
    public string rewiredPrefab;
    public string controller;

    public RewiredCustomButtonAttribute(string prefab, string tag)
    {
        this.rewiredPrefab = prefab;
        this.controller = tag;
    }
}
