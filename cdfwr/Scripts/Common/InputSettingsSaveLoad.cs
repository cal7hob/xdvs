using UnityEngine;

public class InputSettingsSaveLoad : MonoBehaviour
{

    #region Property
    public int TurretTypeIndex
    {
        get { return _turretTypeIndex; }
        set
        {
            PlayerPrefs.SetFloat("TurretControlType", value);
            _turretTypeIndex = value;
        }
    }

    public float TurretSensibility
    {
        get { return _turretSensibility; }
        set
        {
            PlayerPrefs.SetFloat("TurretSensibility", value);
            _turretSensibility = value;
        }
    }

    public float DeadZoneX
    {
        get { return _deadZoneX; }
        set
        {
            PlayerPrefs.SetFloat("MoveJDeadZoneX", value);
            _deadZoneX = value;
        }
    }

    public float DeadZoneY
    {
        get { return _deadZoneY; }
        set
        {
            PlayerPrefs.SetFloat("MoveJDeadZoneY", value);
            _deadZoneY = value;
        }
    }

    public float SensetivityX
    {
        get { return _sensetivityX; }
        set
        {
            PlayerPrefs.SetFloat("MoveJXSensibility", value);
            _sensetivityX = value;
        }
    }

    public float SensetivityY
    {
        get
        {
            return _sensetivityY;
        }
        set
        {
            PlayerPrefs.SetFloat("MoveJYSensibility", value);
            _sensetivityY = value;
        }
    }
    #endregion
    
    #region const
    private const int TurretTypeIndexDefault = 0;
    private const float TurretSensibilityDefault = 0.5f;
    private const float DeadZoneXDefault = 0.07f;
    private const float DeadZoneYDefault = 0.3f;
    private const float SensetivityXDefault = 0.5f;
    private const float SensitivityYDefault = 0.5f;
    #endregion

    #region temp
    private float _turretSensibility;
    private float _sensetivityX;
    private float _sensetivityY;
    private float _deadZoneX;
    private float _deadZoneY;
    private int _turretTypeIndex;
    #endregion

    #region Methods
    void Awake()
    {
        _turretSensibility = PlayerPrefs.HasKey("TurretSensibility") ? PlayerPrefs.GetFloat("TurretSensibility") : TurretSensibilityDefault;
        _sensetivityX = PlayerPrefs.HasKey("MoveJXSensibility") ? PlayerPrefs.GetFloat("MoveJXSensibility") : SensetivityXDefault;
        _sensetivityY = PlayerPrefs.HasKey("MoveJYSensibility") ? PlayerPrefs.GetFloat("MoveJYSensibility") : SensitivityYDefault;
        _deadZoneX = PlayerPrefs.HasKey("MoveJDeadZoneX") ? PlayerPrefs.GetFloat("MoveJDeadZoneX") : DeadZoneXDefault;
        _deadZoneY = PlayerPrefs.HasKey("MoveJDeadZoneY") ? PlayerPrefs.GetFloat("MoveJDeadZoneY") : DeadZoneYDefault;
        _turretTypeIndex = PlayerPrefs.HasKey("TurretControlType")
            ? PlayerPrefs.GetInt("TurretControlType")
            : TurretTypeIndexDefault;
    }

    public void ResetToDefault()
    {
        PlayerPrefs.SetFloat("TurretSensibility", TurretSensibilityDefault);
        PlayerPrefs.SetFloat("MoveJXSensibility", SensetivityXDefault);
        PlayerPrefs.SetFloat("MoveJYSensibility", SensitivityYDefault);
        PlayerPrefs.SetFloat("MoveJDeadZoneX", DeadZoneXDefault);
        PlayerPrefs.SetFloat("MoveJDeadZoneY", DeadZoneYDefault);
        PlayerPrefs.SetInt("TurretControlType", TurretTypeIndexDefault);
        Awake();
    }
    #endregion
}
