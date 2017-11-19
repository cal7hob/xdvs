using System.Linq;
using UnityEngine;

public class MaterialSwitcherAuto : MonoBehaviour
{
    public MaterialSwitcher.Setting[] settings;

    private MeshRenderer meshRenderer;
    private GraphicsLevel currentGraphicsLevel;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        Dispatcher.Subscribe(EventId.QualityLevelChanged, OnQualityLevelChanged);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.QualityLevelChanged, OnQualityLevelChanged);
    }

    void OnEnable()
    {
        Switch();
    }

    private void OnQualityLevelChanged(EventId id, EventInfo ei)
    {
        Switch();
    }

    private void Switch()
    {
        GraphicsLevel newGraphicsLevel = Settings.GraphicsLevel;
        
        if (newGraphicsLevel == currentGraphicsLevel)
            return;

        currentGraphicsLevel = newGraphicsLevel;

        foreach (MaterialSwitcher.Setting setting in settings.OrderByDescending(s => (int)s.minGraphicsLevel))
        {
            if (Settings.GraphicsLevel >= setting.minGraphicsLevel)
            {
                meshRenderer.sharedMaterial = setting.material;
                //MaterialManager.RegisterMaterial(setting.material);
                break;
            }
        }
    }
}
