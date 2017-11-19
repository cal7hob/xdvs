using System;
using UnityEngine;

public class SoundControllerSpaceship : SoundControllerBase
{
    private const float MIN_COLLISION_EXIT_SOUND_GAP = 0.5f;
    private const float COLLISION_SENSITIVITY = 6.0f;

    private SpaceshipController spaceshipController;
    private float lastCollisionEnterTime;
    private float lastCollisionExitTime;
    private AudioWrapper frictionAudio;
    private AudioWrapper engineAudio;
    private AudioWrapper[] audioWrappers;

    void Awake()
    {
        spaceshipController = GetComponent<SpaceshipController>();

        Dispatcher.Subscribe(EventId.TankJoinedBattle, OnTankJoinedBattle);
        Dispatcher.Subscribe(EventId.TankKilled, OnTankKilled);
        Dispatcher.Subscribe(EventId.SettingsSubmited, OnSettingsSubmitted);
        Dispatcher.Subscribe(EventId.TankRespawned, OnTankRespawned);
        Dispatcher.Subscribe(EventId.TankAvailabilityChanged, OnTankAvailabilityChanged);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TankJoinedBattle, OnTankJoinedBattle);
        Dispatcher.Unsubscribe(EventId.TankKilled, OnTankKilled);
        Dispatcher.Unsubscribe(EventId.SettingsSubmited, OnSettingsSubmitted);
        Dispatcher.Unsubscribe(EventId.TankRespawned, OnTankRespawned);
        Dispatcher.Unsubscribe(EventId.TankAvailabilityChanged, OnTankAvailabilityChanged);
    }

    void Update()
    {
        if (spaceshipController.IsMain && spaceshipController.IsAvailable)
            SetEngineNoise(spaceshipController.AccelerationProgress);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!spaceshipController.IsMain)
            return;

        if (collision.relativeVelocity.magnitude < COLLISION_SENSITIVITY)
            return;

        lastCollisionEnterTime = Time.time;

        PlayCollisionSound(collision: collision, collisionExit: false);
        PlayFrictionSound(true);
    }

    void OnCollisionExit(Collision collision)
    {
        if (!spaceshipController.IsMain)
            return;

        lastCollisionExitTime = Time.time;

        PlayCollisionSound(collision: collision, collisionExit: true);
        PlayFrictionSound(false);
    }

    private void OnTankJoinedBattle(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;

        int playerId = info.int1;

        if (spaceshipController.data.playerId != playerId || !spaceshipController.IsMain)
            return;

        SetAudioSources();

        if (!engineAudio.IsPlaying)
            engineAudio.Play();
    }

    private void OnTankKilled(EventId id, EventInfo ei)
    {
        EventInfo_III info = (EventInfo_III)ei;

        int victimId = info.int1;

        if (victimId != spaceshipController.data.playerId || !spaceshipController.IsMain)
            return;

        foreach (AudioWrapper audioWrapper in audioWrappers)
            if (audioWrapper.IsPlaying)
                audioWrapper.Stop();
    }

    private void OnSettingsSubmitted(EventId id, EventInfo ei)
    {
        if (!spaceshipController.IsMain)
            return;

        foreach (AudioWrapper audioWrapper in audioWrappers)
            audioWrapper.UpdateVolume();
    }

    private void OnTankRespawned(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;

        int playerId = info.int1;

        if (spaceshipController.data.playerId != playerId || !spaceshipController.IsMain)
            return;

        if (!engineAudio.IsPlaying)
            engineAudio.Play();
    }

    private void OnTankAvailabilityChanged(EventId id, EventInfo ei)
    {
        if (audioWrappers == null)
            return;

        EventInfo_II info = (EventInfo_II)ei;

        int playerId = info.int1;
        bool available = Convert.ToBoolean(info.int2);

        if (spaceshipController.data.playerId != playerId || available || !spaceshipController.IsMain)
            return;

        foreach (AudioWrapper audioWrapper in audioWrappers)
            if (audioWrapper.IsPlaying)
                audioWrapper.Stop(1.0f);
    }

    private void SetAudioSources()
    {
        engineAudio = SetAudioSource(spaceshipController.engineSound, true, ENGINE_VOLUME);
        frictionAudio = SetAudioSource(spaceshipController.frictionSounds.GetRandomItem(), true, COLLISION_VOLUME);

        audioWrappers = new[]
        {
            frictionAudio,
            engineAudio
        };
    }

    private void PlayFrictionSound(bool collisionEnter)
    {
        if (!collisionEnter && frictionAudio.IsPlaying)
            frictionAudio.Stop();

        if (collisionEnter && !frictionAudio.IsPlaying)
            frictionAudio.Play();
    }

    private void PlayCollisionSound(Collision collision, bool collisionExit)
    {
        AudioClip collisionSound;

        if (MiscTools.CheckIfLayerInMask(spaceshipController.OthersLayerMask, collision.gameObject.layer) && !collisionExit)
            collisionSound = spaceshipController.shipCollisionSounds.GetRandomItemOrDefault();
        else if (collisionExit && lastCollisionExitTime - lastCollisionEnterTime > MIN_COLLISION_EXIT_SOUND_GAP)
            collisionSound = spaceshipController.defaultCollisionExitSounds.GetRandomItemOrDefault();
        else if (collisionExit)
            collisionSound = null;
        else
            collisionSound = spaceshipController.defaultCollisionEnterSounds.GetRandomItemOrDefault();

        if (collisionSound != null && !AudioDispatcher.IsPlaying(collisionSound))
            AudioDispatcher.PlayClipAtPosition(
                clip:       collisionSound,
                position:   transform.position,
                volume:     COLLISION_VOLUME);
    }

    private void SetEngineNoise(float t)
    {
        if (engineAudio == null)
            return;

        engineAudio.Pitch = Mathf.Lerp(spaceshipController.minEnginePitch, spaceshipController.maxEnginePitch, t);
        engineAudio.Volume = Mathf.Lerp(spaceshipController.minEngineVolume, spaceshipController.maxEngineVolume, t);
    }
}
