using System;
using UnityEngine;

namespace XD
{
    [Serializable]
    public class VoiceClip
    {
        [SerializeField]
        private string          name = "";
        [SerializeField]
        private VoiceEventKey   eventKey = VoiceEventKey.NotEnoughFuel;        
        //[SerializeField]
        //private AudioClip[]     variantSounds = null;
        [SerializeField]
        private SFXEffect[]     variants = null;

        [SerializeField]
        private float           intervalLimitSeconds = 0;
        [Range(0, 1)]
        [SerializeField]
        private float           priority = 0;
        [SerializeField]
        private int             triesLimit = 0;        

        private const float     DEFAULT_VOICE_VOLUME_RATIO = 2.0f; // Умножается на Settings.SoundVolume. Но, в любом случае, будет обрезано до 1.

        private float           lastPlayedTime = 0;
        private int             tries = 0;
        private AudioSource     source = null;

        //public AudioClip[] VariantsSounds
        //{
        //    get
        //    {
        //        return variantSounds;
        //    }
        //}

        public SFXEffect[] Variants
        {
            get
            {
                return variants;
            }

            set
            {
                variants = value;
            }
        }

        public VoiceEventKey EventKey
        {
            get
            {
                return eventKey;
            }
        }

        public float Priority
        {
            get
            {
                return priority;
            }
        }

        public bool IsPlaying
        {
            get
            {
                return source.isPlaying;
            }
        }

        public bool CheckPlaybackAvailability()
        {
            if (Time.time < lastPlayedTime + intervalLimitSeconds)
            {
                return false;
            }

            if (++tries < triesLimit && tries != 1)
            {
                return false;
            }

            if (tries >= triesLimit)
            {
                tries = 0;
            }

            return true;
        }

        public void Play(AudioSource source)
        {
            this.source = source;            
            source.clip = variants.GetRandomItem().Clip;
            source.Play();
            lastPlayedTime = Time.time;

            //currentSound = variantSounds.GetRandomItem();
            //if (!StaticContainer.SceneManager.InBattle)
            //{
            //    AudioDispatcher.PlayClip(currentSound);
            //}
            //else
            //{
            //    if (BattleCamera.Instance == null)
            //    {
            //        return;
            //    }
            //
            //    AudioDispatcher.PlayClipAtPosition(
            //        /* clip:    */  currentSound,
            //        /* volume:  */  DEFAULT_VOICE_VOLUME_RATIO,
            //        /* channel: */  AudioSourceInsert.Channel.Voice,
            //        /* parent:  */  BattleCamera.Instance.transform);
            //}
        }

        public void Stop()
        {
            if (source != null)
            {
                source.Stop();
            }
            //AudioDispatcher.Stop(currentSound);
        }
    }
}