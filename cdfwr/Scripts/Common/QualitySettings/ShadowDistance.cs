using UnityEngine;
using System;

public class ShadowDistance : MonoBehaviour {

    [SerializeField, QualityName]
    string qualityName;
    [SerializeField]
    float shadowDistance;

    [SerializeField]
    bool restoreOnDestroy = false;
    [SerializeField, ShowInLabel]
    float oldDistance = 0;

    void Awake () {
        Dispatcher.Subscribe (EventId.SettingsSubmited, OnSettingsChanged);
    }

    void OnDestroy () {
        Dispatcher.Unsubscribe (EventId.SettingsSubmited, OnSettingsChanged);
        if (restoreOnDestroy) {
            QualitySettings.shadowDistance = oldDistance;
        }
    }

    // Use this for initialization
    void Start () {
        UpdateShadowDistance ();

    }

    void OnSettingsChanged (EventId ev, EventInfo info) {
        UpdateShadowDistance ();
    }

    void UpdateShadowDistance () {
        oldDistance = QualitySettings.shadowDistance;
        var ind = Array.IndexOf (QualitySettings.names, qualityName);
        if ((ind >= 0) && (QualitySettings.GetQualityLevel () == ind)) {
            QualitySettings.shadowDistance = shadowDistance;
        }
    }

}
