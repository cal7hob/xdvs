#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class JoystickManager : MonoBehaviour
{
    [Header("Ссылки")]
    public JoystickController[] joysticks;
    private Rect[] allButtonsRect;
    [Header("Коэффициенты для акселерометра")]
    [Range(0.0f, 10.0f)]
    public float horizontalGyroQualifier;
    [Range(0.0f, 10.0f)]
    public float verticalGyroQualifier;

    public static JoystickManager Instance { get; private set; }

    public float HorizontalGyroQualifier { get { return horizontalGyroQualifier; } }

    public float VerticalGyroQualifier { get { return verticalGyroQualifier; } }

    public JoystickController[] Items { get { return joysticks; } }

    public enum Joystics
    {
        left,
        right
    }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        Instance = null;
    }
}

