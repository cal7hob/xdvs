using System.Collections.Generic;
using UnityEngine;

public class VoiceManager : MonoBehaviour
{
    [SerializeField]
    private bool useDelay = false;
    [SerializeField]
    [Range(0f,3f)]
    private float delayBefore = 0f;
    public VoiceLocale[] locales;

    private static VoiceManager instance;

    private VoiceClip previousClip;
    private Dictionary<Localizer.LocalizationLanguage, Dictionary<VoiceEventKey, VoiceClip>> clips;

    public static bool UseDelay 
    {
        get { return instance.useDelay; }
    }

    public static float DelayBefore 
    {
        get { return instance.delayBefore; }
    }

    void Awake()
    {
        instance = this;
        CacheClipsDictionary();
    }

    void OnDestroy()
    {
        instance = null;
    }

    public static void Play(VoiceEventKey eventKey)
    {
        if (BattleController.Instance != null &&
            BattleController.MyVehicle.IsCrashing &&
            eventKey != VoiceEventKey.Crashing)
        {
            return;
        }

        Dictionary<VoiceEventKey, VoiceClip> localeClips;

        if (!instance.clips.TryGetValue((Localizer.LocalizationLanguage)ProfileInfo.languageIndex, out localeClips))
        { return; }

        VoiceClip clip;

        if (!localeClips.TryGetValue(eventKey, out clip))
        {
            return;
        }

        bool playbackAvailable = clip.CheckPlaybackAvailability();

        if (instance.previousClip != null && instance.previousClip.IsPlaying)
        {
            if (instance.previousClip.priority > clip.priority || !playbackAvailable)
                return;

            instance.previousClip.Stop();
        }

        if (!playbackAvailable)
        {
            return;
        }

        clip.Play();

        instance.previousClip = clip;
    }

    private void CacheClipsDictionary()
    {
        clips = new Dictionary<Localizer.LocalizationLanguage, Dictionary<VoiceEventKey, VoiceClip>>();

        foreach (VoiceLocale locale in locales)
        {
            foreach (VoiceClip clip in locale.clips)
            {
                if (!clips.ContainsKey(locale.language))
                {
                    clips.Add(locale.language, new Dictionary<VoiceEventKey, VoiceClip> { { clip.eventKey, clip } });
                }
                else
                {
                    clips[locale.language][clip.eventKey] = clip;
                }
            }
        }
    }
}
