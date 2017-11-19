using System;
using UnityEngine;

public class BattleSettings : MonoBehaviour
{
    public static BattleSettings Instance { get; private set; }

    [SerializeField] private GameObject wrapper;
    [SerializeField] private tk2dUIScrollableArea scrollableArea;
    [SerializeField] private tk2dUIScrollbar musicVolumeScrollbar;
    [SerializeField] private tk2dUIScrollbar soundVolumeScrollbar;
    [SerializeField] private tk2dUIScrollbar camToTankDistanceScrollbar;
    [SerializeField] private tk2dUIScrollbar turretRotationSensitivityScrollbar;
    [SerializeField] private tk2dUIToggleControl voiceDisabled;
    [SerializeField] private tk2dUIToggleControl backwardInversion;
    [SerializeField] private tk2dUIToggleControl turretControlTypeCheckbox;//выключен = FloatingX, включен = Speed
    [SerializeField] private tk2dUIToggleControl fixTurret;

    private float soundVolume;
    private float turretRotationSensitivity;
    private float camToTankScrollValue;
    private float camToTankDefaultDistance;
    private FloatSpeedXYJoystick.TypeJoystick turretControlType = Settings.DEFAULT_TURRET_CONTROL_TYPE;

    public bool FixateTurretDirection { get; private set; }

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

        Messenger.Subscribe(EventId.StatTableVisibilityChange, OnStatTableChangeVisibility);
        Messenger.Subscribe(EventId.MessageBoxChangeVisibility, OnMessageBoxChangeVisibility);
        Messenger.Subscribe(EventId.NotifierChangeVisibility, OnNotifierChangeVisibility);
        Messenger.Subscribe(EventId.MainTankAppeared, OnMainVehicleAppeared, 4);
        Messenger.Subscribe(EventId.BattleChatCommandsChangeVisibility, OnBattleChatCommandsChangeVisibility);

        //Инициализация параметров

        FixateTurretDirection = Convert.ToBoolean(PlayerPrefs.GetInt("FixateTurret", Settings.DEFAULT_FIX_TURRET_VALUE));
        fixTurret.IsOn = FixateTurretDirection;

        turretRotationSensitivity = PlayerPrefs.HasKey("TurretRotationSensitivity") ? PlayerPrefs.GetFloat("TurretRotationSensitivity") : Settings.DEFAULT_TURRET_ROTATION_SENSITIVITY;
        turretControlType = PlayerPrefs.HasKey("TurretControlType") ? (FloatSpeedXYJoystick.TypeJoystick)PlayerPrefs.GetInt("TurretControlType") : Settings.DEFAULT_TURRET_CONTROL_TYPE;

        musicVolumeScrollbar.Value = Settings.MusicVolume * Settings.SOUND_VOLUME_SCROLLER_MULTIPLIER;
        soundVolumeScrollbar.Value = Settings.SoundVolume * Settings.SOUND_VOLUME_SCROLLER_MULTIPLIER;

        musicVolumeScrollbar.OnScroll += SetMusicVolume;
        soundVolumeScrollbar.OnScroll += SetSoundVolume;

        if (turretRotationSensitivityScrollbar)
            turretRotationSensitivityScrollbar.Value = turretRotationSensitivity;

        if (camToTankDistanceScrollbar)
            camToTankDistanceScrollbar.Value = camToTankScrollValue;

        voiceDisabled.gameObject.SetActive(ProfileInfo.IsVoiceDisablingAvailable);
        if(voiceDisabled.gameObject.activeSelf)
            voiceDisabled.IsOn = ProfileInfo.isVoiceDisabled;

        backwardInversion.IsOn = ProfileInfo.isInvert;

        if(turretControlTypeCheckbox)
            turretControlTypeCheckbox.IsOn = turretControlType == FloatSpeedXYJoystick.TypeJoystick.speed;
    }

    void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.StatTableVisibilityChange, OnStatTableChangeVisibility);
        Messenger.Unsubscribe(EventId.MessageBoxChangeVisibility, OnMessageBoxChangeVisibility);
        Messenger.Unsubscribe(EventId.NotifierChangeVisibility, OnNotifierChangeVisibility);
        Messenger.Unsubscribe(EventId.MainTankAppeared, OnMainVehicleAppeared);
        Messenger.Unsubscribe(EventId.BattleChatCommandsChangeVisibility, OnBattleChatCommandsChangeVisibility);

        musicVolumeScrollbar.OnScroll -= SetMusicVolume;
        soundVolumeScrollbar.OnScroll -= SetSoundVolume;

        if (camToTankDistanceScrollbar)
            camToTankDistanceScrollbar.OnScroll -= OnChangeCamToTankDistance;

        Instance = null;
    }

    private void OnMainVehicleAppeared(EventId id, EventInfo info)
    {
        if(camToTankDistanceScrollbar == null)
            return;

        camToTankDistanceScrollbar.OnScroll += OnChangeCamToTankDistance;

        camToTankDefaultDistance = Mathf.Abs(BattleController.MyVehicle.Turret.InverseTransformPoint(BattleController.MyVehicle.ForCam.position).z);

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
        Settings.MusicVolume = musicVolumeScrollbar.Value / Settings.SOUND_VOLUME_SCROLLER_MULTIPLIER;
        //Debug.LogError("SetMusicVolume " + musicVolume);
        MusicBox.Volume = Settings.MusicVolume;
    }

    private void SetSoundVolume(tk2dUIScrollbar scrollbar)
    {
        Settings.SoundVolume = soundVolumeScrollbar.Value / Settings.SOUND_VOLUME_SCROLLER_MULTIPLIER;
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
        PlayerPrefs.SetFloat("MusicVolume", Settings.MusicVolume);
        PlayerPrefs.SetFloat("SoundVolume", Settings.SoundVolume);
        PlayerPrefs.SetFloat("TurretRotationSensitivity", turretRotationSensitivity);
        PlayerPrefs.SetFloat("CamToTankScrollValue", camToTankScrollValue);

        if (turretControlTypeCheckbox)
        {
            turretControlType = turretControlTypeCheckbox.IsOn ? FloatSpeedXYJoystick.TypeJoystick.speed : FloatSpeedXYJoystick.TypeJoystick.floatingX;
            PlayerPrefs.SetInt("TurretControlType", (int)turretControlType);
        }
        
        ProfileInfo.isInvert = backwardInversion.IsOn;
        if (voiceDisabled.gameObject.activeSelf)
            ProfileInfo.isVoiceDisabled = voiceDisabled.IsOn;

        FixateTurretDirection = fixTurret.IsOn;
        PlayerPrefs.SetInt("FixateTurret", Convert.ToInt32(FixateTurretDirection));

        PlayerPrefs.Save();

        Messenger.Send(EventId.BattleSettingsSubmited, null);
    }

    private void OpenBattleSettings(tk2dUIItem btn)
    {
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

        if (en)
        {
            Instance.scrollableArea.Value = 0;
            Instance.scrollableArea.ContentLength = Instance.scrollableArea.MeasureContentLength();
            //Debug.LogError("Instance.scrollableArea.ContentLength = " + Instance.scrollableArea.ContentLength);
        }

        Messenger.Send(EventId.BattleSettingsChangeVisibility, new EventInfo_B(en));
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
