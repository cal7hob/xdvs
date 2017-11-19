using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FXInfo : ISerializationCallbackReceiver
{
    [HideInInspector] [SerializeField] private string[] fxResourcesHigh = new string[5];
    [HideInInspector] [SerializeField] private string[] fxResourcesLow = new string[5];

    public string GetResourcePath(bool highDetailed)
    {
        string[] fxStrings = highDetailed ? fxResourcesHigh : fxResourcesLow;
        int qualityLevel = QualityManager.CurrentQualityLevel;
        string result = fxStrings[qualityLevel];

        if (!string.IsNullOrEmpty(result))
        {
            return result;
        }

        for (int i = qualityLevel; i >= 0; --i)
        {
            result = fxStrings[i];
            if (!string.IsNullOrEmpty(result))
            {
                fxStrings[qualityLevel] = result;
                return result;
            }
        }

        for (int i = qualityLevel + 1; i < fxResourcesLow.Length; ++i)
        {
            result = fxStrings[i];

            if (!string.IsNullOrEmpty(result))
            {
                fxStrings[qualityLevel] = result;
                return result;
            }
        }

        return null;
    }

    public void OnBeforeSerialize()
    {
    }

    public void OnAfterDeserialize()
    {
        if (fxResourcesHigh.Length != 0)
            return;

        fxResourcesHigh = new string[5];
        fxResourcesLow = new string[5];
    }
}
