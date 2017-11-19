using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapQualityCutter : MonoBehaviour
{
    private static bool initialized = false;

    public int enableFogSinceLevel = 1;

    void Awake()
    {
        if (initialized)
        {
            Debug.LogError("There is more than one MapQualityCutter on scene!!!");
        }

        initialized = true;
        Work();
    }

    void OnDestroy()
    {
        initialized = false;
    }

    private void Work()
    {
        RenderSettings.fog = QualityManager.CurrentQualityLevel >= enableFogSinceLevel;
    }
}
