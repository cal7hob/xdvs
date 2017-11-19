using UnityEngine;
using System.Collections;
using Rewired;
#if UNITY_EDITOR
using UnityEditor;
#endif
public class JoystickAllScreen : MonoBehaviour {

    public enum Mode { TouchPad, Speed }

    [Header("Оси для Rewired Custom Controller"), SerializeField]
    private string horizontalAxisKey = "Right Stick X";
    [SerializeField]
    private string verticalAxisKey = "Right Stick Y";

    [Header("Режим работы"), SerializeField]
    private Mode mode = Mode.TouchPad;
    [SerializeField]
    private bool mouseSupport = true;

    public bool IsOn { get; set; }

    private CustomController touchController;

    // Use this for initialization
    void Start () {
        touchController = XDevs.Input.TouchController;
        ReInput.InputSourceUpdateEvent += ReInput_InputSourceUpdateEvent;
    }

    void OnDestroy()
    {
        ReInput.InputSourceUpdateEvent -= ReInput_InputSourceUpdateEvent;
    }

    private void ReInput_InputSourceUpdateEvent()
    {
        if (!IsOn) return;
        //touchController.SetAxisValue(horizontalAxisKey, GetXAxis());
        //touchController.SetAxisValue(verticalAxisKey, GetYAxis());
    }

    // Update is called once per frame
    void Update () {
    
    }



#if UNITY_EDITOR
    void OnSceneGUI() {
        Handles.BeginGUI();
        Handles.EndGUI();
    }
#endif
}
