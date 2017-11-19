using UnityEngine;

public class BattleSettings : MonoBehaviour
{
    public static BattleSettings Instance { get; private set; }

    public GameObject wrapper;

    private const float SOUND_VOLUME_SCROLLER_MULTIPLIER = 2.0f;

    private float musicVolume;
    private float soundVolume;
    private float turretRotationSensitivity;
    private float camToTankScrollValue;
    private float camToTankDefaultDistance;

    public float SoundVolume
    {
        get { return soundVolume; }
    }

    public float TurretRotationSensitivity
    {
        get { return turretRotationSensitivity; }
    }

    public float CamToTankScrollValue
    {
        get { return camToTankScrollValue; }
    }

    void Awake()
    {
        Instance = this;
        wrapper.SetActive(false);//не делать SetActive(false);

        Dispatcher.Subscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);
        Dispatcher.Subscribe(EventId.MessageBoxChangeVisibility, OnMessageBoxChangeVisibility);
        Dispatcher.Subscribe(EventId.OnNotifierChangeVisibility, OnNotifierChangeVisibility);
        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainVehicleAppeared);

        //Инициализация параметров
        musicVolume = PlayerPrefs.HasKey("MusicVolume") ? PlayerPrefs.GetFloat("MusicVolume") : Settings.DEFAULT_MUSIC_VOLUME;
        soundVolume = PlayerPrefs.HasKey("SoundVolume") ? PlayerPrefs.GetFloat("SoundVolume") : Settings.DEFAULT_SOUND_VOLUME;

        turretRotationSensitivity = PlayerPrefs.HasKey("TurretRotationSensitivity") ? PlayerPrefs.GetFloat("TurretRotationSensitivity") : Settings.DEFAULT_TURRET_ROTATION_SENSITIVITY;
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);
        Dispatcher.Unsubscribe(EventId.MessageBoxChangeVisibility, OnMessageBoxChangeVisibility);
        Dispatcher.Unsubscribe(EventId.OnNotifierChangeVisibility, OnNotifierChangeVisibility);
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainVehicleAppeared);

        Instance = null;

    }

    private void OnMainVehicleAppeared(EventId id, EventInfo info)
    {
        camToTankDefaultDistance = Mathf.Abs(XD.StaticContainer.BattleController.CurrentUnit.Turret.transform.InverseTransformPoint(XD.StaticContainer.BattleController.CurrentUnit.CameraPoint.position).z);

        camToTankScrollValue = PlayerPrefs.HasKey("CamToTankScrollValue")
            ? PlayerPrefs.GetFloat("CamToTankScrollValue")
            : Settings.DEFAULT_CAM_TO_TANK_DISTANCE;
    }

    /*private void OnChangeCamToTankDistance(tk2dUIScrollbar scrollbar)
    {
        // S = (max - min) * Scroll.Value + min

        camToTankScrollValue = scrollbar.Value;
        var pos = XD.StaticContainer.BattleController.CurrentUnit.CameraPoint.localPosition; 
        pos.z = -1 * (camToTankDefaultDistance * scrollbar.Value);
        var defCamSpeed = GroundCamera.DefaultCamSpeed;
        GroundCamera.Instance.camSpeed = Mathf.Clamp(defCamSpeed / camToTankScrollValue, defCamSpeed, defCamSpeed * 3);
        XD.StaticContainer.BattleController.CurrentUnit.CameraPoint.localPosition = pos;
        GroundCamera.Instance.SetCamDeltaPosition(XD.StaticContainer.BattleController.CurrentUnit.CameraPoint.position);
    }*/

    /*private void SetMusicVolume(tk2dUIScrollbar scrollbar)
    {
    }

    private void SetSoundVolume(tk2dUIScrollbar scrollbar)
    {
    }

    private void SetTurretRotationSensitivity(tk2dUIScrollbar scrollbar)
    {
    }

    private void OnSubmitClick(tk2dUIItem btn)
    {
        SetActive(false);
    }*/

    private void ApplyBattleSettings()
    {
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SoundVolume", soundVolume);
        PlayerPrefs.SetFloat("TurretRotationSensitivity", turretRotationSensitivity);
        PlayerPrefs.SetFloat("CamToTankScrollValue", camToTankScrollValue);
        PlayerPrefs.Save();
        Dispatcher.Send(EventId.BattleSettingsSubmited, null);
    }

    /*private void OpenBattleSettings(tk2dUIItem btn)
    {
        SetActive(!OnScreen);
    }*/

    private void OnStatTableChangeVisibility(EventId id, EventInfo info)
    {
        if(((EventInfo_B)info).bool1)
            SetActive(false);
    }

    private void OnMessageBoxChangeVisibility(EventId id, EventInfo info)
    {
        if(((EventInfo_B)info).bool1)
            SetActive(false);
    }

    private void OnNotifierChangeVisibility(EventId id, EventInfo info)
    {
        if (((EventInfo_B)info).bool1 && OnScreen)
            SetActive(false);
    }

    public static void SetActive(bool en)
    {
        if (Instance == null)
            return;
        if(OnScreen && !en)//Вместо OnDisable у wrapper
            Instance.ApplyBattleSettings();
        Instance.wrapper.SetActive(en);
        Dispatcher.Send(EventId.OnBattleSettingsChangeVisibility, new EventInfo_B(en));
    }

    public static bool OnScreen
    {
        get
        {
            if (Instance == null)
                return false;
            return Instance.wrapper.activeSelf;
        }
    }
}
