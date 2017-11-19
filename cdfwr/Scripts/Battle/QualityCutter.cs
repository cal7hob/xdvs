using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

public class QualityCutter : MonoBehaviour
{
    [Serializable]
    public class QualityCutterSetting
    {
        public int qualityLevel;
        public GameObject[] objectsToEnable;
        public GameObject[] objectsToDisable;
    }

    public QualityCutterSetting[] settings;

    private QualityCutterSetting currentSetting;

    void Awake()
    {
        Dispatcher.Subscribe(EventId.QualityLevelChanged, OnQualityLevelChanged);
    }
    
    void Start()
    {
        //Refresh();
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.QualityLevelChanged, OnQualityLevelChanged);
    }

    private void Refresh()
    {
        FindCurrentSetting();
        if (currentSetting == null)
        {
            return;
        }
        foreach (GameObject obj in currentSetting.objectsToDisable) 
        {
            if (obj.GetComponent<VehicleController>()) 
            {
                continue;
            }
            Destroy(obj);
        }
        //MiscTools.SetObjectsActivity(currentSetting.objectsToDisable, false);
        MiscTools.SetObjectsActivity(currentSetting.objectsToEnable, true);
    }

    private void FindCurrentSetting()
    {
        /*Выбирает настройку с уровнем, равным тек. уровню качества,
        либо ближайший более низкий (либо первый из списка, если таковых нет).*/
        
        if (settings == null || settings.Length == 0)
        {
            DT.LogError(gameObject, "Quality cutter settings not found!");
            currentSetting = null;
            return;
        }

        int qualityLevel = QualitySettings.GetQualityLevel();
        int delta = 0;
        int minDelta = 0;
        currentSetting = settings[0];
        for(int i = 1; i < settings.Length; i++)
        {
            if (settings[i].qualityLevel == qualityLevel)
            {
                currentSetting = settings[i];
                return;
            }

            delta = settings[i].qualityLevel - qualityLevel;
            if (delta < 0 && delta > minDelta)
            {
                minDelta = delta;
                currentSetting = settings[i];
            }
        }
    }

    private void OnQualityLevelChanged(EventId eid, EventInfo info)
    {
        Refresh();
    }
}
