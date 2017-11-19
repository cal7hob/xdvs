using UnityEngine;
using System;

public class BattleSettings : MonoBehaviour
{
    public static BattleSettings Instance { get; private set; }

    [SerializeField] private GameObject wrapper;
    [SerializeField] private tk2dUIScrollbar musicVolumeScrollbar;
    [SerializeField] private tk2dUIScrollbar soundVolumeScrollbar;
    [SerializeField] private tk2dUIScrollbar camToTankDistanceScrollbar;
    [SerializeField] private tk2dUIScrollbar turretRotationSensitivityScrollbar;
    [SerializeField] private tk2dUIToggleControl voiceDisabled;
    [SerializeField] private tk2dUIToggleControl backwardInversion;
    [SerializeField] private tk2dUIToggleControl turretControlTypeCheckbox;//выключен = FloatingX, включен = Speed
    [SerializeField] private tk2dUIToggleControl autoAimingTypeCheckbox;

    private const float SOUND_VOLUME_SCROLLER_MULTIPLIER = 2.0f;

    private float soundVolume;
    private float turretRotationSensitivity;
    private float camToTankScrollValue;
    private float camToTankDefaultDistance;
    private FloatSpeedXYJoystick.TypeJoystick turretControlType = Settings.DEFAULT_TURRET_CONTROL_TYPE;

    public float SoundVolume
    {
        get
        {
            return soundVolume;
        }
        set
        {
            if (!HelpTools.Approximately(value, soundVolume))
                Dispatcher.Send(EventId.SoundVolumeChanged, new EventInfo_SimpleEvent());

            soundVolume = value;
        }
    }

    public float MusicVolume
    {
        get; set;
    }

    public float TurretRotationSensitivity
    {
        get { return turretRotationSensitivity; }
    }

    public float CamToTankScrollValue
    {
        get { return camToTankScrollValue; }
    }

    public FloatSpeedXYJoystick.TypeJoystick TurretControlType
    {
        get { return turretControlType; }
    }

    void Awake()
    {
        Instance = this;
        wrapper.SetActive(false);//не делать SetActive(false);

        Dispatcher.Subscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);
        Dispatcher.Subscribe(EventId.MessageBoxChangeVisibility, OnMessageBoxChangeVisibility);
        Dispatcher.Subscribe(EventId.OnNotifierChangeVisibility, OnNotifierChangeVisibility);
        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainVehicleAppeared, 4);
        Dispatcher.Subscribe(EventId.OnBattleChatCommandsChangeVisibility, OnBattleChatCommandsChangeVisibility);

        //Инициализация параметров
        MusicVolume = PlayerPrefs.HasKey("MusicVolume") ? PlayerPrefs.GetFloat("MusicVolume") : Settings.DEFAULT_MUSIC_VOLUME;
        SoundVolume = PlayerPrefs.HasKey("SoundVolume") ? PlayerPrefs.GetFloat("SoundVolume") : Settings.DEFAULT_SOUND_VOLUME;

        turretRotationSensitivity = PlayerPrefs.HasKey("TurretRotationSensitivity") ? PlayerPrefs.GetFloat("TurretRotationSensitivity") : Settings.DEFAULT_TURRET_ROTATION_SENSITIVITY;
        turretControlType = PlayerPrefs.HasKey("TurretControlType") ? (FloatSpeedXYJoystick.TypeJoystick)PlayerPrefs.GetInt("TurretControlType") : Settings.DEFAULT_TURRET_CONTROL_TYPE;

        musicVolumeScrollbar.Value = MusicVolume * SOUND_VOLUME_SCROLLER_MULTIPLIER;
        soundVolumeScrollbar.Value = SoundVolume * SOUND_VOLUME_SCROLLER_MULTIPLIER;

        if (turretRotationSensitivityScrollbar)
            turretRotationSensitivityScrollbar.Value = turretRotationSensitivity;

        if (camToTankDistanceScrollbar)
            camToTankDistanceScrollbar.Value = camToTankScrollValue;

        voiceDisabled.gameObject.SetActive(ProfileInfo.IsVoiceDisablingAvailable);
        if(voiceDisabled.gameObject.activeSelf)
            voiceDisabled.IsOn = ProfileInfo.isVoiceDisabled;

        backwardInversion.IsOn = ProfileInfo.isInvert;

        if (turretControlTypeCheckbox)
            turretControlTypeCheckbox.IsOn = turretControlType == FloatSpeedXYJoystick.TypeJoystick.speed;

        if (autoAimingTypeCheckbox)
        {
            autoAimingTypeCheckbox.IsOn = System.Convert.ToBoolean(ProfileInfo.AutoAimingType);
            autoAimingTypeCheckbox.gameObject.SetActive(SystemInfo.deviceType == DeviceType.Handheld);
        }
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);
        Dispatcher.Unsubscribe(EventId.MessageBoxChangeVisibility, OnMessageBoxChangeVisibility);
        Dispatcher.Unsubscribe(EventId.OnNotifierChangeVisibility, OnNotifierChangeVisibility);
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainVehicleAppeared);
        Dispatcher.Unsubscribe(EventId.OnBattleChatCommandsChangeVisibility, OnBattleChatCommandsChangeVisibility);

        Instance = null;

        if (camToTankDistanceScrollbar)
            camToTankDistanceScrollbar.OnScroll -= OnChangeCamToTankDistance;
    }

    private void OnMainVehicleAppeared(EventId id, EventInfo info)
    {
        if(camToTankDistanceScrollbar == null)
            return;

        camToTankDistanceScrollbar.OnScroll += OnChangeCamToTankDistance;

        camToTankDefaultDistance = Mathf.Abs(BattleController.MyVehicle.Turret.transform.InverseTransformPoint(BattleController.MyVehicle.ForCam.position).z);

        camToTankScrollValue = PlayerPrefs.HasKey("CamToTankScrollValue")
            ? PlayerPrefs.GetFloat("CamToTankScrollValue")
            : Settings.DEFAULT_CAM_TO_TANK_DISTANCE;

        camToTankDistanceScrollbar.Value = camToTankScrollValue;
    }

    private void OnChangeCamToTankDistance(tk2dUIScrollbar scrollbar)
    {
        camToTankScrollValue = scrollbar.Value;
        var myVehicle = BattleController.MyVehicle;
        myVehicle.ForCam.localPosition = myVehicle.cameraEndPoint.localPosition - myVehicle.CameraTranslationAxis * camToTankDefaultDistance * scrollbar.Value;
        GroundCamera.GroundCamInstance.SetCamDeltaPosition(BattleController.MyVehicle.ForCam.position);
    }

    private void SetMusicVolume(tk2dUIScrollbar scrollbar)
    {
        MusicVolume = musicVolumeScrollbar.Value / SOUND_VOLUME_SCROLLER_MULTIPLIER;
        //Debug.LogError("SetMusicVolume " + musicVolume);
        MusicBox.Volume = MusicVolume;
    }

    private void SetSoundVolume(tk2dUIScrollbar scrollbar)
    {
        SoundVolume = soundVolumeScrollbar.Value / SOUND_VOLUME_SCROLLER_MULTIPLIER;
        //Debug.LogError("SetSoundVolume " + soundVolume);
    }

    private void SetTurretRotationSensitivity(tk2dUIScrollbar scrollbar)
    {
        if(turretRotationSensitivityScrollbar)
            turretRotationSensitivity = turretRotationSensitivityScrollbar.Value;
        //Debug.LogError("SetTurretRotationSensitivity " + turretRotationSensitivity);
    }

    private void OnSubmitClick(tk2dUIItem btn)
    {
        SetActive(false);
    }

    private void ApplyBattleSettings()
    {
        PlayerPrefs.SetFloat("MusicVolume", MusicVolume);
        PlayerPrefs.SetFloat("SoundVolume", SoundVolume);
        PlayerPrefs.SetFloat("TurretRotationSensitivity", turretRotationSensitivity);
        PlayerPrefs.SetFloat("CamToTankScrollValue", camToTankScrollValue);
        if (turretControlTypeCheckbox)
        {
            turretControlType = turretControlTypeCheckbox.IsOn ? FloatSpeedXYJoystick.TypeJoystick.speed : FloatSpeedXYJoystick.TypeJoystick.floatingX;
            PlayerPrefs.SetInt("TurretControlType", (int)turretControlType);
        }

        if(autoAimingTypeCheckbox)
            ProfileInfo.AutoAimingType = autoAimingTypeCheckbox.IsOn ? AutoAimingType.DefaultAutoAiming : AutoAimingType.WithoutAutoAiming;

        PlayerPrefs.Save();
        ProfileInfo.isInvert = backwardInversion.IsOn;
        if (voiceDisabled.gameObject.activeSelf)
            ProfileInfo.isVoiceDisabled = voiceDisabled.IsOn;
        Dispatcher.Send(EventId.BattleSettingsSubmited, null);
    }

    private void OpenBattleSettings(tk2dUIItem btn)
    {
        #region Testing HightPingAlarm
        //Dispatcher.Send(EventId.HighPingAlarm, new EventInfo_B(true));
        //return;
        #endregion

        SetActive(!OnScreen);
    }

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

    private void OnBattleChatCommandsChangeVisibility(EventId id, EventInfo info)
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
