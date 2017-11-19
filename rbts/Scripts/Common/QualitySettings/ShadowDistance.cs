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
        Messenger.Subscribe (EventId.SettingsSubmited, OnSettingsChanged);
    }

    void OnDestroy () {
        Messenger.Unsubscribe (EventId.SettingsSubmited, OnSettingsChanged);
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
