using UnityEngine;

public class JoystickManager : MonoBehaviour
{
    [Header("Ссылки")]
    public JoystickController[] joysticks;
    [SerializeField]
    private AbstractClassForButtons[] AllButtonOnSceneForDeadZones;
    [SerializeField]
    private FloatSpeedXYJoystick ScreenJoystick;
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

    void Start()
    {
        ReplaceButtons();
    }

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
    /// <summary>
    ///  расставляем мёртвые зоны для кнопок заново. Запускается в том числе и при изменении разрешения экрана, из скрипта FloatSpeedXYJoystick.
    /// </summary>
    public void ReplaceButtons()
    {
        if (ScreenJoystick != null)
        {
            ScreenJoystick.ClearAreaExcepts();

            foreach (var deadZones in AllButtonOnSceneForDeadZones)
            {
                ScreenJoystick.AddAreaExcepts(deadZones.Coord());
            }
        }
    }

    //Методы для отображения мёртвых зон для джойстика поворота башни. Включаем для откладки мёртвых зон. 
    /*
    Rect reYcoordforOnGUI(Rect coord)
    {
        return new Rect(coord.x, Screen.height - coord.yMax, coord.width, coord.height);
    }
    void OnGUI()
    {
        Color color = Color.blue;
        foreach (var deadZones in AllButtonOnSceneForDeadZones)
        {
            EditorGUI.DrawRect(reYcoordforOnGUI(deadZones.Coord()), color);
        }

    }*/
}

