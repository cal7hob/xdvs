using System.Linq;
using UnityEngine;

public class MaterialSwitcherAuto : MonoBehaviour
{
    public MaterialSwitcher.Setting[] settings;

    private MeshRenderer meshRenderer;
    private GraphicsLevel _graphicsLevel;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        Messenger.Subscribe(EventId.QualityLevelChanged, OnQualityLevelChanged);
    }

    void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.QualityLevelChanged, OnQualityLevelChanged);
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
        
        if (newGraphicsLevel == _graphicsLevel)
            return;

        _graphicsLevel = newGraphicsLevel;

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
