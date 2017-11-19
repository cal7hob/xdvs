using System.Collections;
using UnityEngine;

public class SoundControllerBase : MonoBehaviour
{
    public const float DYNAMIC_ENGINE_PITCH_MIN         = 1.0f;
    public const float DYNAMIC_ENGINE_PITCH_MAX         = 1.15f;
    public const float DYNAMIC_ENGINE_PITCH_MIN_AR      = 1.0f;
    public const float DYNAMIC_ENGINE_PITCH_MAX_AR      = 1.1f;
    public const float DYNAMIC_ENGINE_VOLUME_MIN        = 0.5f;
    public const float DYNAMIC_ENGINE_VOLUME_MAX        = 1.0f;
    public const float TURRET_ROTATION_VOLUME           = 0.55f;
    public const float SHOT_VOLUME                      = 1.10f; //1.20f;
    public const float EXPLOSION_VOLUME                 = 1.20f; //1.60f;
    public const float COLLISION_VOLUME                 = 1.00f; //0.15f;
    public const float TANK_HIT_VOLUME                  = 1.20f; //1.40f;
    protected const float TURRET_VOLUME                 = 1.00f; //0.65f;
    protected const float ENGINE_VOLUME                 = 1.0f;  //1.20f; // Для SJ.
    protected const float IDLE_VOLUME                   = 1.20f; //1.20f;
    protected const float ACCELERATION_VOLUME           = 0.80f; //0.80f;
    protected const float CATERPILLAR_VOLUME            = 1.20f; //0.65f;
    protected const float ROTATION_VOLUME               = 1.00f; //0.70f;
    protected const float REVERSE_VOLUME                = 1.00f; //1.20f;
    protected const float RELOADING_VOLUME              = 0.50f; //0.40f;
    protected const float RELOADING_SUPERWEAPON_VOLUME  = 1.50f; //1.0f;
    protected const float FADING_SPEED                  = 0.6f;

    protected class AudioWrapper
    {
        private readonly float initialSelfVolume;
        private readonly AudioSource source;
        private bool isStarting;
        private bool isStopping;
        private bool isResuming;
        private float selfVolume;

        public AudioWrapper(AudioSource audioSource, float maxVolume)
        {
            source = audioSource;
            initialSelfVolume = maxVolume;
            Volume = initialSelfVolume;
        }

        public bool IsPlaying
        {
            get { return source.isPlaying; }
        }

        public bool IsStarting
        {
            get { return isStarting; }
        }

        public bool IsStopping
        {
            get { return isStopping; }
        }

        public float Volume
        {
            get
            {
                return selfVolume;
            }
            set
            {
                selfVolume = value;
                source.volume = selfVolume * Settings.SoundVolume;
            }
        }

        public float Pitch
        {
            get { return source.pitch; }
            set { source.pitch = value; }
        }

        public void UpdateVolume()
        {
            Volume = selfVolume;
        }

        public void Play()
        {
            source.Play();
        }

        public void Stop()
        {
            source.Stop();
        }

        public void Play(float fadingSpeed)
        {
            CoroutineHelper.Start(Starting(fadingSpeed));
        }

        public void Stop(float fadingSpeed)
        {
            CoroutineHelper.Start(Stopping(fadingSpeed));
        }

        public void Resume(float fadingSpeed)
        {
            isResuming = true;
            CoroutineHelper.Start(Starting(fadingSpeed));
        }

        private IEnumerator Starting(float fadingSpeed)
        {
            if (source == null)
                yield break;

            isStarting = true;

            Volume = 0;

            if (!isResuming)
                source.Play();

            while (Volume < Mathf.Clamp01(initialSelfVolume))
            {
                if ((isStopping && !isResuming) || source == null)
                {
                    isStarting = false;
                    isResuming = false;
                    yield break;
                }

                Volume += fadingSpeed * Time.deltaTime;

                yield return null;
            }

            isStarting = false;
            isResuming = false;
        }

        private IEnumerator Stopping(float fadingSpeed)
        {
            if (source == null)
                yield break;

            isStopping = true;

            while (Volume > 0)
            {
                if (isStarting || isResuming || source == null)
                {
                    isStopping = false;
                    yield break;
                }

                Volume -= fadingSpeed * Time.deltaTime;

                yield return null;
            }

            if (source.isPlaying)
                source.Stop();

            Volume = initialSelfVolume;

            isStopping = false;
        }
    }

    protected AudioWrapper SetAudioSource(AudioClip sound, bool loop, float selfVolume)
    {
        AudioSource audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.clip = sound;
        audioSource.loop = loop;
        audioSource.playOnAwake = false;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.maxDistance = AudioDispatcher.MaxDistance;
        audioSource.spread = 179.0f;
        audioSource.spatialBlend = 1.0f;
        audioSource.dopplerLevel = 0;

        return new AudioWrapper(audioSource, selfVolume);
    }
}
