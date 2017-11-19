using System;
using System.Linq;
using UnityEngine;

public class MaterialSwitcher : MonoBehaviour
{
    [Serializable]
    public class Setting
    {
        public GraphicsLevel minGraphicsLevel;
        public Material material;
    }

    public Setting[] settings;
    public Material windowsPhoneMaterialMin; // Костыль для WP.
    public Material windowsPhoneMaterialMax;

    private MeshRenderer meshRenderer;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        //MaterialManager.RegisterMaterial(windowsPhoneMaterialMax);
        //MaterialManager.RegisterMaterial(windowsPhoneMaterialMin);
    }

    public void Switch(GraphicsLevel graphicsLevel)
    {
        #if UNITY_WSA && UNITY_WP_8_1 // Костыль для WP.

        if (graphicsLevel >= GraphicsLevel.highQuality && windowsPhoneMaterialMax != null)
        {
            meshRenderer.sharedMaterial = windowsPhoneMaterialMax;
            return;
        }

        if (windowsPhoneMaterialMin != null)
        {
            meshRenderer.sharedMaterial = windowsPhoneMaterialMin;
            return;
        }

        #endif

        foreach (Setting setting in settings.OrderByDescending(s => (int)s.minGraphicsLevel))
        {
            if (graphicsLevel >= setting.minGraphicsLevel)
            {
                meshRenderer.sharedMaterial = setting.material;
                break;
            }
        }
    }
}
