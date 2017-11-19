using UnityEngine;

public class HandBrakeButton : MonoBehaviour
{
    public tk2dUIItem btnHandBrake;

    private static HandBrakeButton instance;

    public static bool IsPressed { get { return instance.btnHandBrake.IsPressed; } }

    void Awake()
    {
        instance = this;

        #if (UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_WEBGL) && !UNITY_EDITOR
        btnHandBrake.gameObject.SetActive(false);
        #endif
    }

    void OnDestroy() { instance = null; }
}
