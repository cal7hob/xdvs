using UnityEngine;

public class SoundControllerTankAR : SoundControllerBase
{
    private TankControllerAR tankController;
    private AudioWrapper idleAudio;
    private AudioWrapper accelerationAudio;
    private AudioWrapper caterpillarAudio;
    private AudioWrapper reverseAudio;
    private AudioWrapper turretAudio;
    private AudioWrapper[] audioWrappers;
    private EngineState currentEngineState;
    private VehicleRotationState currentRotationState;

    void Awake()
    {
        tankController = GetComponent<TankControllerAR>();

        Dispatcher.Subscribe(EventId.TankJoinedBattle, OnTankJoinedBattle);
        Dispatcher.Subscribe(EventId.EngineStateChanged, OnEngineStateChanged);
        Dispatcher.Subscribe(EventId.VehicleRotationStateChanged, OnVehicleRotationStateChanged);
        Dispatcher.Subscribe(EventId.WeaponReloaded, OnTankWeaponReloaded);
        Dispatcher.Subscribe(EventId.StartTurretRotation, OnStartTurretRotation);
        Dispatcher.Subscribe(EventId.StopTurretRotation, OnStopTurretRotation);
        Dispatcher.Subscribe(EventId.TankKilled, OnTankKilled);
        Dispatcher.Subscribe(EventId.SettingsSubmited, OnSettingsSubmitted);
        Dispatcher.Subscribe(EventId.BattleSettingsSubmited, OnSettingsSubmitted);
        Dispatcher.Subscribe(EventId.SoundVolumeChanged, OnSoundVolumeChanged);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TankJoinedBattle, OnTankJoinedBattle);
        Dispatcher.Unsubscribe(EventId.EngineStateChanged, OnEngineStateChanged);
        Dispatcher.Unsubscribe(EventId.VehicleRotationStateChanged, OnVehicleRotationStateChanged);
        Dispatcher.Unsubscribe(EventId.WeaponReloaded, OnTankWeaponReloaded);
        Dispatcher.Unsubscribe(EventId.StartTurretRotation, OnStartTurretRotation);
        Dispatcher.Unsubscribe(EventId.StopTurretRotation, OnStopTurretRotation);
        Dispatcher.Unsubscribe(EventId.TankKilled, OnTankKilled);
        Dispatcher.Unsubscribe(EventId.SettingsSubmited, OnSettingsSubmitted);
        Dispatcher.Unsubscribe(EventId.BattleSettingsSubmited, OnSettingsSubmitted);
        Dispatcher.Unsubscribe(EventId.SoundVolumeChanged, OnSoundVolumeChanged);
    }

    void Update()
    {
        SetPitch();
    }

    private void OnTankJoinedBattle(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;

        int playerId = info.int1;

        if (tankController.data.playerId != playerId || !tankController.IsMain)
            return;

        SetAudioSources();
    }

    private void OnEngineStateChanged(EventId id, EventInfo ei)
    {
        EventInfo_II info = (EventInfo_II)ei;

        int playerId = info.int1;

        if (playerId == tankController.data.playerId && tankController.IsMain)
        {
            currentEngineState = (EngineState)info.int2;
            SwitchEngineSound(currentEngineState);
        }
    }

    private void OnVehicleRotationStateChanged(EventId id, EventInfo ei)
    {
        EventInfo_II info = (EventInfo_II)ei;

        int playerId = info.int1;

        if (playerId == tankController.data.playerId && tankController.IsMain)
        {
            currentRotationState = (VehicleRotationState)info.int2;
            SwitchEngineSound(currentRotationState);
        }
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

        foreach (AudioWrapper audioWrapper in audioWrappers)
            if (audioWrapper.IsPlaying)
                audioWrapper.Stop();
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

    private void SetAudioSources()
    {
        idleAudio = SetAudioSource(tankController.idleSound, true, IDLE_VOLUME);
        accelerationAudio = SetAudioSource(tankController.accelerationSound, false, ACCELERATION_VOLUME);
        caterpillarAudio = SetAudioSource(tankController.trackSound, true, CATERPILLAR_VOLUME);
        reverseAudio = SetAudioSource(tankController.reverseSound, false, REVERSE_VOLUME);
        turretAudio = SetAudioSource(tankController.turretRotationSound, true, TURRET_VOLUME);

        audioWrappers = new[]
        {
            idleAudio,
            accelerationAudio,
            caterpillarAudio,
            reverseAudio,
            turretAudio
        };
    }

    private void SwitchEngineSound(EngineState engineState)
    {
        switch (engineState)
        {
            // Loops:
            case EngineState.Idle:
            case EngineState.ForwardBrake:
            case EngineState.BackwardBrake:
                if (caterpillarAudio.IsPlaying &&
                    !caterpillarAudio.IsStopping &&
                    currentRotationState == VehicleRotationState.Idle)
                {
                    caterpillarAudio.Stop(1.0f);
                }

                if (reverseAudio.IsPlaying &&
                    !reverseAudio.IsStopping &&
                    currentRotationState == VehicleRotationState.Idle)
                {
                    reverseAudio.Stop(1.0f);
                }

                if (accelerationAudio.IsPlaying && !accelerationAudio.IsStopping)
                    accelerationAudio.Stop();

                if (!idleAudio.IsPlaying)
                    idleAudio.Play();
                break;

            case EngineState.Movement:
                if (caterpillarAudio.IsStopping)
                    caterpillarAudio.Resume(2.5f);

                if (!caterpillarAudio.IsPlaying)
                    caterpillarAudio.Play(5.0f);
                break;

            case EngineState.ReverseMovement:
                if (reverseAudio.IsStopping)
                    reverseAudio.Resume(2.5f);

                if (!reverseAudio.IsPlaying)
                    reverseAudio.Play(5.0f);
                break;

            // One-shots:
            case EngineState.ForwardAcceleration:
            case EngineState.BackwardAcceleration:
                if (idleAudio.IsPlaying && !idleAudio.IsStopping)
                    idleAudio.Stop();

                if (engineState == EngineState.ForwardAcceleration &&
                    reverseAudio.IsPlaying &&
                    !reverseAudio.IsStopping)
                {
                    reverseAudio.Stop(1.0f);
                }

                if (engineState == EngineState.BackwardAcceleration &&
                    caterpillarAudio.IsPlaying &&
                    !caterpillarAudio.IsStopping)
                {
                    caterpillarAudio.Stop(1.0f);
                }

                if (accelerationAudio.IsPlaying)
                    accelerationAudio.Stop();

                if (!accelerationAudio.IsPlaying)
                    accelerationAudio.Play();
                break;
        }
    }

    private void SwitchEngineSound(VehicleRotationState rotationState)
    {
        switch (rotationState)
        {
            case VehicleRotationState.Left:
            case VehicleRotationState.Right:
                if (currentEngineState == EngineState.Idle ||
                    currentEngineState == EngineState.ForwardBrake ||
                    currentEngineState == EngineState.BackwardBrake)
                {
                    if (caterpillarAudio.IsStopping)
                        caterpillarAudio.Resume(2.5f);

                    if (reverseAudio.IsStopping)
                        reverseAudio.Resume(2.5f);

                    if (!caterpillarAudio.IsPlaying)
                        caterpillarAudio.Play(5.0f);
                }
                break;

            case VehicleRotationState.Idle:
                if (currentEngineState == EngineState.Idle)
                {
                    if (caterpillarAudio.IsPlaying && !caterpillarAudio.IsStopping)
                        caterpillarAudio.Stop(1.0f);

                    if (reverseAudio.IsPlaying && !reverseAudio.IsStopping)
                        reverseAudio.Stop(1.0f);
                }
                break;
        }
    }

    private void SetPitch()
    {
        if (!tankController.IsMain)
            return;

        caterpillarAudio.Pitch = Mathf.Lerp(DYNAMIC_ENGINE_PITCH_MIN_AR, DYNAMIC_ENGINE_PITCH_MAX_AR, Mathf.Abs(tankController.XAxisAcceleration));
        reverseAudio.Pitch = Mathf.Lerp(DYNAMIC_ENGINE_PITCH_MIN_AR, DYNAMIC_ENGINE_PITCH_MAX_AR, Mathf.Abs(tankController.XAxisAcceleration));
    }
}
