using System;
using UnityEngine;

public class SoundControllerTankMF : SoundControllerBase
{
    private TankControllerMF tankController;
    private AudioWrapper turretAudio;
    private AudioWrapper[] audioWrappers;

    private EngineSound engine;


    public AnimationCurve enginePitch = new AnimationCurve ();

    //pitch of the engine whilst not accelerating
    public float idlePitch = 0.7f;
    //the target pitch to attain when the vehicle is moving forwards
    public float fullAccelPitch = 1.4f;
    //the target pitch to attain when the vehicle is moving backwards
    public float fullReversePitch = 1.2f;
    public float rateOfPitchChange = .12f;

    private float  tempEngineAudioPitch;
    private float verticalInput;
    public float carSpeedQ = 0f;

    private AudioClip SuperWeaponReloadingSound
    {
        get
        {
            switch (tankController.SuperWeaponInfo.SuperWeaponType)
            {
                case SuperWeaponType.ATGW:
                    return tankController.atgwReloadingSound;
                case SuperWeaponType.MachineGun:
                    return tankController.machinegunReloadingSound;
                case SuperWeaponType.AGS:
                    return tankController.agsReloadingSound;
                default:
                    return null;
            }
        }
    }

    void Awake ()
    {
        tankController = GetComponent<TankControllerMF>();
        engine = GetComponent<EngineSound> ();

        Dispatcher.Subscribe(EventId.TankJoinedBattle, OnTankJoinedBattle);
        Dispatcher.Subscribe(EventId.WeaponReloaded, OnTankWeaponReloaded);
        Dispatcher.Subscribe(EventId.StartTurretRotation, OnStartTurretRotation);
        Dispatcher.Subscribe(EventId.StopTurretRotation, OnStopTurretRotation);
        Dispatcher.Subscribe(EventId.TankKilled, OnTankKilled);
        Dispatcher.Subscribe(EventId.SettingsSubmited, OnSettingsSubmitted);
        Dispatcher.Subscribe(EventId.BattleSettingsSubmited, OnSettingsSubmitted);
        Dispatcher.Subscribe(EventId.TankRespawned, OnVehicleRespawned);
        Dispatcher.Subscribe(EventId.SoundVolumeChanged, OnSoundVolumeChanged);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TankJoinedBattle, OnTankJoinedBattle);
        Dispatcher.Unsubscribe(EventId.WeaponReloaded, OnTankWeaponReloaded);
        Dispatcher.Unsubscribe(EventId.StartTurretRotation, OnStartTurretRotation);
        Dispatcher.Unsubscribe(EventId.StopTurretRotation, OnStopTurretRotation);
        Dispatcher.Unsubscribe(EventId.TankKilled, OnTankKilled);
        Dispatcher.Unsubscribe(EventId.SettingsSubmited, OnSettingsSubmitted);
        Dispatcher.Unsubscribe(EventId.BattleSettingsSubmited, OnSettingsSubmitted);
        Dispatcher.Unsubscribe(EventId.TankRespawned, OnVehicleRespawned);
        Dispatcher.Unsubscribe(EventId.SoundVolumeChanged, OnSoundVolumeChanged);
    }

    private void Update () {
        carSpeedQ = Mathf.Abs (tankController.speedKmH / tankController.MaxSpeed);
        //engineAudio.Volume = Mathf.Lerp (0.2f, .35f, carSpeedQ);
        verticalInput = tankController.YAxisControl;
        tempEngineAudioPitch = Mathf.Lerp (.4f, 1.40f, carSpeedQ);
        var accelPitch = enginePitch.Evaluate (carSpeedQ);
        if (verticalInput == 0) {
            //The vehicle is idling so we want to set the pitch to idlePitch, but we don't want the pitch to just jump, so we use
            //Mathf.MoveTowards to gradually 'move' audio.pitch towards idlePitch, by rateOfPitchChange. Where pitchModifier is the rate of change.
            //engineAudio.Pitch = Mathf.MoveTowards (engineAudio.Pitch, idlePitch * tempEngineAudioPitch * 1.35f, rateOfPitchChange);
            engine.Pitch = Mathf.MoveTowards (engine.Pitch, idlePitch * tempEngineAudioPitch * 1.35f, rateOfPitchChange);
        }
        else if (verticalInput > 0) //if the user has pressed the 'w' key or Up Arrow on the keyboard 
        {
            //basicly the same as above, just moves the pitch towards movingForwardsPitch
            //engineAudio.Pitch = Mathf.MoveTowards (engineAudio.Pitch, fullAccelPitch * tempEngineAudioPitch, rateOfPitchChange);
            //engineAudio.Pitch = Mathf.MoveTowards (engineAudio.Pitch, fullAccelPitch * accelPitch, rateOfPitchChange);
            engine.Pitch = Mathf.MoveTowards (engine.Pitch, fullAccelPitch * accelPitch, rateOfPitchChange);
        }
        else if (verticalInput < 0) // if the user has pressed the 's' key or Down Arrow on the keyboard 
        {
            //moves pitch towards movingBackwardsPitch 
            //engineAudio.Pitch = Mathf.MoveTowards (engineAudio.Pitch, fullReversePitch * tempEngineAudioPitch, rateOfPitchChange);
            //engineAudio.Pitch = Mathf.MoveTowards (engineAudio.Pitch, fullReversePitch * accelPitch, rateOfPitchChange);
            engine.Pitch = Mathf.MoveTowards (engine.Pitch, fullReversePitch * accelPitch, rateOfPitchChange);
        }
        if (CheckSuperWeaponReloading()) {
            AudioDispatcher.PlayClipAtPosition(SuperWeaponReloadingSound, transform.position, RELOADING_SUPERWEAPON_VOLUME);
        }
    }

    private void OnTankJoinedBattle(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;

        int playerId = info.int1;

        if (tankController.data.playerId != playerId || !tankController.IsMain)
            return;

        SetAudioSources();
    }

    private void OnTankWeaponReloaded(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;

        GunShellInfo.ShellType shellType = (GunShellInfo.ShellType)info.int1;

        if (shellType == GunShellInfo.ShellType.Usual)
            AudioDispatcher.PlayClipAtPosition(tankController.reloadingSound, transform.position, RELOADING_VOLUME);
    }

    private void OnStopTurretRotation(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;

        int playerId = info.int1;

        if (playerId != tankController.data.playerId || !tankController.IsMain)
            return;

        if (turretAudio.IsPlaying)
            turretAudio.Stop();
    }

    private void OnStartTurretRotation(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;

        int playerId = info.int1;

        if (playerId != tankController.data.playerId || !tankController.IsMain)
            return;

        if (!turretAudio.IsPlaying)
            turretAudio.Play();
    }

    private void OnTankKilled(EventId id, EventInfo ei)
    {
        EventInfo_III info = (EventInfo_III)ei;

        int victimId = info.int1;

        if (victimId != tankController.data.playerId || !tankController.IsMain)
            return;

        foreach (AudioWrapper audioWrapper in audioWrappers) {
            if (audioWrapper.IsPlaying) {
                audioWrapper.Stop ();
            }
        }
        engine.Stop ();
    }

    private void OnSettingsSubmitted(EventId id, EventInfo info)
    {
        SetVolume();
    }

    private void OnSoundVolumeChanged(EventId id, EventInfo info)
    {
        SetVolume();
    }

    private void SetVolume()
    {
        if (!tankController.IsMain)
            return;

        foreach (AudioWrapper audioWrapper in audioWrappers)
            audioWrapper.UpdateVolume();
    }

    private void OnVehicleRespawned (EventId id, EventInfo info) {
        EventInfo_I eventInfo = (EventInfo_I)info;

        int playerId = eventInfo.int1;

        if (playerId == tankController.data.playerId) {
            engine.Play ();
        }
    }

    private bool CheckSuperWeaponReloading()
    {
        return  tankController.IsMain &&
                tankController.SuperWeaponBC != null &&
                tankController.SuperWeaponBC.Reloaded &&
                SuperWeaponReloadingSound != null;
    }

    private void SetAudioSources()
    {
        turretAudio = SetAudioSource(tankController.turretRotationSound, true, TURRET_VOLUME);

        audioWrappers = new[]
        {
            turretAudio
        };
    }

}
