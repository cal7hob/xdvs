using UnityEngine;

public class CubemapSwitcher : MonoBehaviour
{
    public Shader patternShader;
    public string cubemapPropertyKey;
    public Cubemap cubemap;

    void Awake()
    {
        Dispatcher.Subscribe(EventId.QualitySettingsChanged, OnQualitySettingsChanged);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.QualitySettingsChanged, OnQualitySettingsChanged);
    }

    void Start()
    {
        Switch();
    }

    private void OnQualitySettingsChanged(EventId id, EventInfo ei)
    {
        Switch();
    }

    private void Switch()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(includeInactive: true);

        foreach (Renderer childRenderer in renderers)
        {
            if (childRenderer == null)
                continue;

            Material[] childMaterials = childRenderer.materials;

            foreach (Material childMaterial in childMaterials)
                if (childMaterial.shader == patternShader)
                    childMaterial.SetTexture(cubemapPropertyKey, cubemap);
        }
    }
}
