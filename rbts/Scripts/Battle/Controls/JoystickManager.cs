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

    public JoystickController[] Items { get { return joysticks; } }

    void Start()
    {
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
        Messenger.Subscribe(EventId.DeadZoneObjectStateChanged, OnDeadZoneObjectStateChanged);
        Messenger.Subscribe(EventId.ResolutionChanged, OnResolutionChanged);
    }

    void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.DeadZoneObjectStateChanged, OnDeadZoneObjectStateChanged);
        Messenger.Unsubscribe(EventId.ResolutionChanged, OnResolutionChanged);
        Instance = null;
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

    #region Методы для отображения мёртвых зон для джойстика поворота башни. Включаем для откладки мёртвых зон.

    //Rect reYcoordforOnGUI(Rect coord)
    //{
    //    return new Rect(coord.x, Screen.height - coord.yMax, coord.width, coord.height);
    //}

    //void OnGUI()
    //{
    //    Color color = new Color(0, 0, 0.4f, 0.5f);
    //    foreach (var deadZoneObject in Instance.deadZoneObjects)
    //    {
    //        UnityEditor.EditorGUI.DrawRect(reYcoordforOnGUI(deadZoneObject.GetDeadZone()), color);
    //    }
    //}

    #endregion

    private void OnDeadZoneObjectStateChanged(EventId id, EventInfo ei)
    {
        EventInfo_U eInfoU = (EventInfo_U)ei;
        IDeadZone iDeadZoneObject = (IDeadZone)eInfoU[0];
        bool state = (bool)eInfoU[1];

        //Debug.LogErrorFormat("OnDeadZoneObjectStateChanged: {0}, {1}", iDeadZoneObject, state);

        if (state && !deadZoneObjects.Contains(iDeadZoneObject))
        {
            deadZoneObjects.Add(iDeadZoneObject);
            UpdateDeadZones();
        }
        else if (!state && deadZoneObjects.Contains(iDeadZoneObject))
        {
            deadZoneObjects.Remove(iDeadZoneObject);
            UpdateDeadZones();
        }
    }

    private void OnResolutionChanged(EventId id, EventInfo ei)
    {
        UpdateDeadZones();
    }
}
