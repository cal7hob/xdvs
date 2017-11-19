using System.Collections.Generic;
using UnityEngine;

public class JoystickManager : MonoBehaviour
{
    [Header("Ссылки")]
    public JoystickController[] joysticks;
    [SerializeField] private FloatSpeedXYJoystick ScreenJoystick;
    private Rect[] allButtonsRect;
    [Header("Коэффициенты для акселерометра")]
    [Range(0.0f, 10.0f)]
    public float horizontalGyroQualifier;
    [Range(0.0f, 10.0f)]
    public float verticalGyroQualifier;

    private HashSet<IDeadZone> deadZoneObjects = new HashSet<IDeadZone>();

    public static JoystickManager Instance { get; private set; }

    public float HorizontalGyroQualifier { get { return horizontalGyroQualifier; } }

    public float VerticalGyroQualifier { get { return verticalGyroQualifier; } }

    void Start()
    {
        foreach (JoystickController joystick in joysticks)
            joystick.Visible = joystick.IsOn && ProfileInfo.ControlOption != ControlOption.gyroscope;

        UpdateDeadZones();
    }

    public enum Joystics
    {
        left,
        right
    }

    void Awake()
    {
        Instance = this;
        Dispatcher.Subscribe(EventId.DeadZoneObjectStateChanged, OnDeadZoneObjectStateChanged);
    }

    void OnDestroy()
    {
        Instance = null;
        Dispatcher.Unsubscribe(EventId.DeadZoneObjectStateChanged, OnDeadZoneObjectStateChanged);
    }
    /// <summary>
    ///  расставляем мёртвые зоны для кнопок заново. Запускается в том числе и при изменении разрешения экрана, из скрипта FloatSpeedXYJoystick.
    /// </summary>
    public static void UpdateDeadZones()
    {
        if (!Instance || !Instance.ScreenJoystick)
            return;

        Instance.ScreenJoystick.ClearAreaExcepts();

        foreach (var deadZoneObject in Instance.deadZoneObjects)
            Instance.ScreenJoystick.AddAreaExcepts(deadZoneObject.GetDeadZone());
    }

    private void OnDeadZoneObjectStateChanged(EventId id, EventInfo ei)
    {
        EventInfo_U eInfoU = (EventInfo_U)ei;
        IDeadZone iDeadZoneObject = (IDeadZone)eInfoU[0];
        bool state = (bool)eInfoU[1];

        if(state && !deadZoneObjects.Contains(iDeadZoneObject))
        {
            deadZoneObjects.Add(iDeadZoneObject);
            UpdateDeadZones();
        }
        else if(!state && deadZoneObjects.Contains(iDeadZoneObject))
        {
            deadZoneObjects.Remove(iDeadZoneObject);
            UpdateDeadZones();
        }
    }
}

