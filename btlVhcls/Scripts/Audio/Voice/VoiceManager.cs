using System.Collections.Generic;
using UnityEngine;

public class VoiceManager : MonoBehaviour
{
    public VoiceLocale[] locales;

    private static VoiceManager instance;

    private VoiceClip previousClip;
    private Dictionary<Localizer.LocalizationLanguage, Dictionary<VoiceEventKey, VoiceClip>> clips;

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
        Play(eventKey, 1.0f);
    }

    public static void Play(VoiceEventKey eventKey, float volume)
    {
        if (BattleController.Instance != null &&
            BattleController.MyVehicle.IsCrashing &&
            eventKey != VoiceEventKey.Crashing)
        {
            return;
        }

        Dictionary<VoiceEventKey, VoiceClip> localeClips; 

        if (!instance.clips.TryGetValue((Localizer.LocalizationLanguage)ProfileInfo.languageIndex, out localeClips))
            return;

        VoiceClip clip;

        if (!localeClips.TryGetValue(eventKey, out clip))
            return;

        bool playbackAvailable = clip.CheckPlaybackAvailability();

        if (instance.previousClip != null && instance.previousClip.IsPlaying)
        {
            if (instance.previousClip.priority > clip.priority || !playbackAvailable)
                return;

            instance.previousClip.Stop();
        }

        if (!playbackAvailable)
            return;

        clip.Play(volume);

        instance.previousClip = clip;
    }

    private void CacheClipsDictionary()
    {
        clips = new Dictionary<Localizer.LocalizationLanguage, Dictionary<VoiceEventKey, VoiceClip>>();

        foreach (VoiceLocale locale in locales)
            foreach (VoiceClip clip in locale.clips)
                if (!clips.ContainsKey(locale.language))
                    clips.Add(locale.language, new Dictionary<VoiceEventKey, VoiceClip> { { clip.eventKey, clip } });
                else
                    clips[locale.language][clip.eventKey] = clip;
    }
}
