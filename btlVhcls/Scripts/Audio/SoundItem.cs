using UnityEngine;

[AddComponentMenu("2D Toolkit/UI/Custom SoundItem")]
public class SoundItem : tk2dUIBaseItemControl
{
    public AudioClip clickButtonSound;
    public bool playOncePerActivation;

    private bool isPlayed;

    void OnEnable()
    {
        if (uiItem && clickButtonSound != null)
            uiItem.OnClick += PlayClickSound;
    }

    void OnDisable()
    {
        if (uiItem && clickButtonSound != null)
            uiItem.OnClick -= PlayClickSound;

        isPlayed = false;
    }

    private void PlayClickSound()
    {
        if (!isPlayed)
            AudioDispatcher.PlayClip(clickButtonSound);

        if (playOncePerActivation)
            isPlayed = true;
    }
}
