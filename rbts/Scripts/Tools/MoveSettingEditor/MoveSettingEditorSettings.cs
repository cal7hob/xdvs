#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class MoveSettingEditorSettings : ScriptableObject//MonoBehaviour
{
    public string searchPath = "Assets"; //Assets/Resources/Prefabs/Vehicles/Boats;Assets/Resources/Prefabs/Vehicles/Boss
    public MonoScript source;
    public List<string> methods = new List<string>();
    public MonoScript target;
    public bool testOnOnePrefab = false;
}
#endif