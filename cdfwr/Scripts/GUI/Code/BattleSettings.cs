using UnityEngine;
using System;

public class BattleSettings : MonoBehaviour
{
    public static BattleSettings Instance { get; private set; }

    public GameObject wrapper;
    public tk2dUIScrollbar musicVolumeScrollbar;
    public tk2dUIScrollbar soundVolumeScrollbar;
    public tk2dUIScrollbar camToTankDistanceScrollbar;
    public tk2dUIScrollbar turretRotationSensitivityScrollbar;
    public tk2dUIToggleControl voiceDisabled;
    public tk2dUIToggleControl backwardInversion;
    public tk2dUIToggleControl turretControlTypeCheckbox;//выключен = FloatingX, включен = Speed
    public GameObject exitButton;
    public GameObject statTable;

    private const float SOUND_VOLUME_SCROLLER_MULTIPLIER = 2.0f;

    private static float musicVolume;
    private static float soundVolume;
    private static float camToTankScrollValue;
    private static float camToTankDefaultDistance;

    public float SoundVolume
    {
        get { return soundVolume; }
    }

    public static float TurretRotationSensitivity { get; private set; }

    public float CamToTankScrollValue
    {
        get { return camToTankScrollValue; }
    }

    void Awake()
    {
        Instance = this;
        wrapper.SetActive(false);//не делать SetActive(false);
#if UNITY_WEBGL || UNITY_STANDALONE || UNITY_WSA
        if (SystemInfo.deviceType != DeviceType.Handheld)
        {
            exitButton.SetActive(true);
        }
#endif
        Dispatcher.Subscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);
        Dispatcher.Subscribe(EventId.MessageBoxChangeVisibility, OnMessageBoxChangeVisibility);
        Dispatcher.Subscribe(EventId.OnNotifierChangeVisibility, OnNotifierChangeVisibility);
        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainVehicleAppeared, 4);
        Dispatcher.Subscribe(EventId.OnBattleChatCommandsChangeVisibility, OnBattleChatCommandsChangeVisibility);

        //Инициализация параметров
        musicVolume = PlayerPrefs.HasKey("MusicVolume") ? PlayerPrefs.GetFloat("MusicVolume") : Settings.DEFAULT_MUSIC_VOLUME;
        soundVolume = PlayerPrefs.HasKey("SoundVolume") ? PlayerPrefs.GetFloat("SoundVolume") : Settings.DEFAULT_SOUND_VOLUME;

        TurretRotationSensitivity = PlayerPrefs.HasKey("TurretRotationSensitivity") ? PlayerPrefs.GetFloat("TurretRotationSensitivity") : Settings.DEFAULT_TURRET_ROTATION_SENSITIVITY;

        musicVolumeScrollbar.Value = musicVolume * SOUND_VOLUME_SCROLLER_MULTIPLIER;
        soundVolumeScrollbar.Value = soundVolume * SOUND_VOLUME_SCROLLER_MULTIPLIER;

        if (turretRotationSensitivityScrollbar)
            turretRotationSensitivityScrollbar.Value = TurretRotationSensitivity;

        if (camToTankDistanceScrollbar)
            camToTankDistanceScrollbar.Value = camToTankScrollValue;

        voiceDisabled.gameObject.SetActive(ProfileInfo.IsVoiceDisablingAvailable);
        if (voiceDisabled.gameObject.activeSelf)
            voiceDisabled.IsOn = ProfileInfo.isVoiceDisabled;

        backwardInversion.IsOn = ProfileInfo.isInvert;

        musicVolumeScrollbar.OnScroll += SetMusicVolume;
        soundVolumeScrollbar.OnScroll += SetSoundVolume;
        turretRotationSensitivityScrollbar.OnScroll += SetTurretRotationSensitivity;
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
        {
            camToTankDistanceScrollbar.OnScroll -= OnChangeCamToTankDistance;
        }
        musicVolumeScrollbar.OnScroll -= SetMusicVolume;
        soundVolumeScrollbar.OnScroll -= SetSoundVolume;
        turretRotationSensitivityScrollbar.OnScroll -= SetTurretRotationSensitivity;
    }

    private void OnMainVehicleAppeared(EventId id, EventInfo info)
    {
        if (camToTankDistanceScrollbar == null)
            return;

        camToTankDistanceScrollbar.OnScroll += OnChangeCamToTankDistance;

        camToTankDefaultDistance = Mathf.Abs(BattleController.MyVehicle.Turret.transform.InverseTransformPoint(BattleController.MyVehicle.CameraPoint.position).z);

        camToTankScrollValue = PlayerPrefs.HasKey("CamToTankScrollValue")
            ? PlayerPrefs.GetFloat("CamToTankScrollValue")
            : Settings.DEFAULT_CAM_TO_TANK_DISTANCE;

        camToTankDistanceScrollbar.Value = camToTankScrollValue;
        OnChangeCamToTankDistance(camToTankDistanceScrollbar);
        ApplyBattleSettings();
    }

    public void OnClickReceiver()
    {
        statTable.SetActive(true);
        statTable.GetComponent<StatTable>().OnExitToHangarClick();
    }

    private void OnChangeCamToTankDistance(tk2dUIScrollbar scrollbar)
    {
        //if (ReferenceEquals(BattleCamera.Instance.VehicleInView, null))
        //{
        //    return;
        //}
        
        camToTankScrollValue = scrollbar.Value;
        var locPos = BattleCamera.Instance.Cam.transform.localPosition;
        locPos.z = -camToTankDefaultDistance * scrollbar.Value;

        BattleCamera.Instance.SetCamDefaultPosition(locPos);
    }

    private void SetMusicVolume(tk2dUIScrollbar scrollbar)
    {
        musicVolume = musicVolumeScrollbar.Value / SOUND_VOLUME_SCROLLER_MULTIPLIER;
        MusicBox.Volume = musicVolume;
    }

    private void SetSoundVolume(tk2dUIScrollbar scrollbar)
    {
        soundVolume = soundVolumeScrollbar.Value / SOUND_VOLUME_SCROLLER_MULTIPLIER;
    }

    private void SetTurretRotationSensitivity(tk2dUIScrollbar scrollbar)
    {
        TurretRotationSensitivity = turretRotationSensitivityScrollbar.Value;
    }

    private void OnSubmitClick(tk2dUIItem btn)
    {
        BattleSoundManager.Instance.PlaySound(BattleSoundManager.Instance.buttonClickSound);
        SetActive(false);
    }

    /* private void OnSubmitClick(tk2dUIItem btn)
     {
         BattleSoundManager.Instance.PlaySound(BattleSoundManager.Instance.buttonClickSound);
         SetActive(false);
     }*/

    private void OnCheckBoxClick(tk2dUIItem box)
    {
        BattleSoundManager.Instance.PlaySound(BattleSoundManager.Instance.checkBoxSound);
    }

    private void ApplyBattleSettings()
    {
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SoundVolume", soundVolume);
        PlayerPrefs.SetFloat("TurretRotationSensitivity", TurretRotationSensitivity);
        PlayerPrefs.SetFloat("CamToTankScrollValue", camToTankScrollValue);

        PlayerPrefs.Save();
        ProfileInfo.isInvert = backwardInversion.IsOn;
        if (voiceDisabled.gameObject.activeSelf)
            ProfileInfo.isVoiceDisabled = voiceDisabled.IsOn;
        Dispatcher.Send(EventId.BattleSettingsSubmited, null);
    }

    private void OpenBattleSettings(tk2dUIItem btn)
    {
        BattleSoundManager.Instance.PlaySound(BattleSoundManager.Instance.buttonClickSound);
        SetActive(!OnScreen); 
    }

    private void OnBattleSettingsBtnPressed()
    {
        Dispatcher.Send(EventId.BattleBtnPressed, new EventInfo_SimpleEvent());
    }

    private void OnStatTableChangeVisibility(EventId id, EventInfo info)
    {
        if (((EventInfo_B)info).bool1 && OnScreen)
            SetActive(false);
    }

    private void OnMessageBoxChangeVisibility(EventId id, EventInfo info)
    {
        if (((EventInfo_B)info).bool1 && OnScreen)
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
        if (OnScreen && !en)//Вместо OnDisable у wrapper
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
