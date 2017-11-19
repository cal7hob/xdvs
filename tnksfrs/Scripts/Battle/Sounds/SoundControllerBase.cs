using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

public class SoundControllerBase : MonoBehaviour
{
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
            get
            {
                return source.isPlaying;
            }
        }

        public bool IsStarting
        {
            get
            {
                return isStarting;
            }
        }

        public bool IsStopping
        {
            get
            {
                return isStopping;
            }
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
            get
            {
                return source.pitch;
            }
            set
            {
                source.pitch = value;
            }
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

    protected AudioWrapper SetAudioSource(AudioClip sound, bool loop, float selfVolume, AudioMixerGroup mixer)
    {
        AudioSource audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.outputAudioMixerGroup = mixer;
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
