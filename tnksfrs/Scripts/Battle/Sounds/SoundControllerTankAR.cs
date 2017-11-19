using System.Collections;
using UnityEngine;

public class SoundControllerTankAR : SoundControllerBase
{
    private class AudioWrapper
    {
        private readonly float          selfVolume;
        private readonly AudioSource    source;

        public bool IsPlaying
        {
            get
            {
                return source.isPlaying;
            }
        }

        public AudioWrapper(AudioSource audioSource, float maxVolume)
        {
            source = audioSource;
            selfVolume = maxVolume;
        }

        public void SetGlobalVolume(float volume)
        {
            source.volume = selfVolume * volume;
        }

        public void Play()
        {
            if (source.clip != null)
            {
                source.Play();
            }
        }

        public void Stop()
        {
            source.Stop();
        }

        public void Play(float fadingSeconds)
        {
            CoroutineHelper.Start(Starting(fadingSeconds));
        }

        public void Stop(float fadingSeconds)
        {
            CoroutineHelper.Start(Stopping(fadingSeconds));
        }

        private IEnumerator Starting(float fadingSpeed)
        {
            source.volume = 0;
            source.Play();

            while (source.volume < 1)
            {
                source.volume += fadingSpeed * Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator Stopping(float fadingSpeed)
        {
            float initialVolume = source.volume;

            while (source.volume > 0)
            {
                if (source == null)
                {
                    yield break;
                }

                source.volume -= fadingSpeed * Time.deltaTime;
                yield return null;
            }

            if (source.isPlaying)
            {
                source.Stop();
            }

            source.volume = initialVolume;
        }
    }

    public const float SHOT_VOLUME          = 1.10f; //1.20f;
    public const float EXPLOSION_VOLUME     = 1.20f; //1.60f;
    public const float COLLISION_VOLUME     = 1.00f; //0.15f;
    public const float TANK_HIT_VOLUME      = 1.20f; //1.40f;
    private const float TURRET_VOLUME       = 1.00f; //0.65f;
    private const float IDLE_VOLUME         = 1.20f; //1.20f;
    private const float ACCELERATION_VOLUME = 0.80f; //0.80f;
    private const float CATERPILLAR_VOLUME  = 1.20f; //0.65f;
    private const float ROTATION_VOLUME     = 1.00f; //0.70f;
    private const float REVERSE_VOLUME      = 1.00f; //1.20f;
    private const float RELOADING_VOLUME    = 0.50f; //0.40f;

    private const float FADING_SPEED = 0.6f;

    private TankControllerAR tankController;
    private AudioWrapper turretAudio;
    private AudioWrapper[] audioWrappers;

    private void Awake()
    {
        tankController = GetComponent<TankControllerAR>();

        Dispatcher.Subscribe(EventId.TankJoinedBattle, OnTankJoinedBattle);
        Dispatcher.Subscribe(EventId.EngineStateChanged, OnEngineStateChanged);
        Dispatcher.Subscribe(EventId.WeaponReloaded, OnTankWeaponReloaded);
        Dispatcher.Subscribe(EventId.StartTurretRotation, OnStartTurretRotation);
        Dispatcher.Subscribe(EventId.StopTurretRotation, OnStopTurretRotation);
        Dispatcher.Subscribe(EventId.TankKilled, OnTankKilled);
        Dispatcher.Subscribe(EventId.SettingsSubmited, OnSettingsSubmitted);
        Dispatcher.Subscribe(EventId.BattleSettingsSubmited, OnSettingsSubmitted);
        Dispatcher.Subscribe(EventId.TankRespawned, OnTankRespawned);
    }

    private void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TankJoinedBattle, OnTankJoinedBattle);
        Dispatcher.Unsubscribe(EventId.EngineStateChanged, OnEngineStateChanged);
        Dispatcher.Unsubscribe(EventId.WeaponReloaded, OnTankWeaponReloaded);
        Dispatcher.Unsubscribe(EventId.StartTurretRotation, OnStartTurretRotation);
        Dispatcher.Unsubscribe(EventId.StopTurretRotation, OnStopTurretRotation);
        Dispatcher.Unsubscribe(EventId.TankKilled, OnTankKilled);
        Dispatcher.Unsubscribe(EventId.SettingsSubmited, OnSettingsSubmitted);
        Dispatcher.Unsubscribe(EventId.BattleSettingsSubmited, OnSettingsSubmitted);
        Dispatcher.Unsubscribe(EventId.TankRespawned, OnTankRespawned);
    }

    private void OnTankJoinedBattle(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;
        int playerId = info.int1;

        if (tankController.data.playerId != playerId || !tankController.IsMine)
        {
            return;
        }

        SetAudioSources();
    }

    private void OnEngineStateChanged(EventId id, EventInfo ei)
    {
        EventInfo_II info = (EventInfo_II)ei;

        int playerId = info.int1;
        EngineState engineState = (EngineState)info.int2;

        if (playerId == tankController.data.playerId && tankController.IsMine)
        {
        }
    }

    private void OnTankWeaponReloaded(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;

        GunShellInfo.ShellType shellType = (GunShellInfo.ShellType)info.int1;

        //if (shellType == GunShellInfo.ShellType.Usual)
        //{
        //    AudioDispatcher.PlayClipAtPosition(tankController.reloadingSound, transform.position, RELOADING_VOLUME);
        //}
    }

    private void OnStopTurretRotation(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;
        int playerId = info.int1;

        if (playerId != tankController.data.playerId || !tankController.IsMine)
        {
            return;
        }

        if (turretAudio == null)
        {
            return;
        }

        if (turretAudio.IsPlaying)
        {
            turretAudio.Stop();
        }
    }

    private void OnStartTurretRotation(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;
        int playerId = info.int1;

        if (playerId != tankController.data.playerId || !tankController.IsMine)
        {
            return;
        }

        if (turretAudio == null)
        {
            return;
        }

        if (!turretAudio.IsPlaying)
        {
            turretAudio.Play();
        }
    }

    private void OnTankKilled(EventId id, EventInfo ei)
    {
        EventInfo_II info = (EventInfo_II)ei;

        int victimId = info.int1;

        if (victimId != tankController.data.playerId || !tankController.IsMine)
            return;

        foreach (AudioWrapper audioWrapper in audioWrappers)
            if (audioWrapper.IsPlaying)
                audioWrapper.Stop();
    }

    private void OnSettingsSubmitted(EventId id, EventInfo info)
    {
        if (!tankController.IsMine)
        {
            return;
        }

        foreach (AudioWrapper audioWrapper in audioWrappers)
        {
            audioWrapper.SetGlobalVolume(Settings.SoundVolume);
        }
    }

    private void OnTankRespawned(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;

        int playerId = info.int1;

        if (tankController.data.playerId != playerId || !tankController.IsMine)
        {
            return;
        }        
    }

    private void SetAudioSources()
    {
        turretAudio = SetAudioSource(tankController.turretRotationSound, true, TURRET_VOLUME);

        audioWrappers = new[]
        {
            turretAudio
        };
    }

    private AudioWrapper SetAudioSource(AudioClip sound, bool loop, float selfVolume)
    {
        AudioSource audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.clip = sound;
        audioSource.volume = Settings.SoundVolume * selfVolume;
        audioSource.loop = loop;
        audioSource.playOnAwake = false;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.maxDistance = 65.0f;
        audioSource.spatialBlend = 1.0f;
        audioSource.dopplerLevel = 0;

        return new AudioWrapper(audioSource, selfVolume);
    }
}