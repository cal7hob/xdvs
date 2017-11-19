using System;
using UnityEngine;

namespace XD
{
    [Serializable]
    public class VoiceLocale
    {
        [SerializeField]
        private SystemLanguage  language = SystemLanguage.Russian;
        [SerializeField]
        private VoiceClip[]     clips = null;

        public SystemLanguage Language
        {
            get
            {
                return language;
            }
        }

        public VoiceClip[] Clips
        {
            get
            {
                return clips;
            }

            set
            {
                clips = value;
            }
        }
    }
}