using UnityEngine;

public class ShadowDistanceSwitcher : MonoBehaviour
{
    public float shadowDistance = 400.0f;

    private float storedShadowDistance;

    void Awake()
    {
        storedShadowDistance = QualitySettings.shadowDistance;
        QualitySettings.shadowDistance = shadowDistance;
    }

    void OnDestroy()
    {
        QualitySettings.shadowDistance = storedShadowDistance;
    }
}
