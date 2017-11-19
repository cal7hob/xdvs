using System.Collections.Generic;
using UnityEngine;

namespace XD
{
    public class VoiceManager : MonoBehaviour, IVoiceManager
    {
        [SerializeField]
        private VoiceLocale[]                                                       locales = null;
        [SerializeField]
        private AudioSource                                                         source = null;
        [SerializeField]
        private bool                                                                debug = false;

        private VoiceClip                                                           previousClip = null;
        private Dictionary<SystemLanguage, Dictionary<VoiceEventKey, VoiceClip>>    clips = null;

        public GameObject GameObject
        {
            get
            {
                return gameObject;
            }
        }

        public VoiceLocale[] Locales
        {
            get
            {
                return locales;
            }

            set
            {
                locales = value;
            }
        }

        #region ISubscriber       
        public string Description
        {
            get
            {
                return "[VoiceManager] " + name;
            }

            set
            {
                name = value;
            }
        }

        public void Reaction(Message message, params object[] parameters)
        {
            switch (message)
            {
                case Message.VoiceRequest:
                    if (debug)
                    {
                        Debug.LogErrorFormat(this, "{0}: {1}", message, parameters.ToFullString());
                    }
                    Play(parameters.Get<VoiceEventKey>());
                    break;
            }
        }
        #endregion

        private void Awake()
        {
            CacheClipsDictionary();
        }

        public void Play(VoiceEventKey eventKey)
        {
            if (BattleController.Instance != null &&
                StaticContainer.BattleController.CurrentUnit.IsCrashing &&
                eventKey != VoiceEventKey.Crashing)
            {
                if (debug)
                {
                    Debug.LogErrorFormat(this, "BattleController.Instance != null && StaticContainer.BattleController.CurrentUnit.IsCrashing && eventKey != VoiceEventKey.Crashing");
                }
                return;
            }

            Dictionary<VoiceEventKey, VoiceClip> localeClips;

            if (!clips.TryGetValue((SystemLanguage)StaticType.Options.Instance<IOptions>().Language, out localeClips))
            {
                if (debug)
                {
                    Debug.LogErrorFormat(this, "Not found Language '{0}'", (SystemLanguage)StaticType.Options.Instance<IOptions>().Language);
                }
                return;
            }

            VoiceClip clip;

            if (!localeClips.TryGetValue(eventKey, out clip))
            {
                return;
            }

            bool playbackAvailable = clip.CheckPlaybackAvailability();

            if (previousClip != null && previousClip.IsPlaying)
            {
                if (previousClip.Priority > clip.Priority || !playbackAvailable)
                {
                    return;
                }

                previousClip.Stop();
            }

            if (!playbackAvailable)
            {
                return;
            }

            clip.Play(source);

            previousClip = clip;
        }

        private void CacheClipsDictionary()
        {
            clips = new Dictionary<SystemLanguage, Dictionary<VoiceEventKey, VoiceClip>>();

            foreach (VoiceLocale locale in locales)
            {
                foreach (VoiceClip clip in locale.Clips)
                {
                    if (!clips.ContainsKey(locale.Language))
                    {
                        clips.Add(locale.Language, new Dictionary<VoiceEventKey, VoiceClip> { { clip.EventKey, clip } });
                    }
                    else
                    {
                        clips[locale.Language][clip.EventKey] = clip;
                    }
                }
            }
        }
    }
}