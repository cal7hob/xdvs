using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MaterialManager : MonoBehaviour
{
    private static HashSet<Material> materialsRegistry = new HashSet<Material>();
    public static void RegisterMaterial(Material material)
    {
        materialsRegistry.Add(material);
    }

    void Awake()
    {
        foreach (Material mat in materialsRegistry)
        {
            Destroy(mat);
        }
        materialsRegistry.Clear();
    }
}
