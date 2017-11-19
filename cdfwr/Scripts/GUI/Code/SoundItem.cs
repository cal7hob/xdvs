using UnityEngine;

[AddComponentMenu("2D Toolkit/UI/Custom SoundItem")]
public class SoundItem : tk2dUIBaseItemControl
{
    public AudioClip clickButtonSound;
    public bool playOncePerActivation;
    [Header("If it's false default meaning's using. HangarController.nextSound")]
    public bool useCustomSound = false;
    private AudioClip currentSound = null;
    public bool skipFirstEvent = false;

    private bool isFirstEvent = true;
    private bool isPlayed;

    void OnEnable()
    {
        //
        if(useCustomSound && currentSound == null)
        {
            return;
        }

        if (uiItem)
        {
            uiItem.OnClick += PlayClickSound;
        }
    }

    void OnDisable()
    {
        if (useCustomSound && currentSound == null)
        {
            return;
        }
        if (uiItem)
        {
            uiItem.OnClick -= PlayClickSound;
        }

        isPlayed = false;
        isFirstEvent = true;
    }

    private void PlayClickSound()
    {
        if (skipFirstEvent && isFirstEvent) 
        {
            isFirstEvent = false;
            return;
        }

        if (currentSound == null) 
        {
            currentSound = (HangarController.Instance == null || useCustomSound) ? clickButtonSound : MenuController.Instance.nextSound;
        }
        
       // Debug.Log("Play click sound " + gameObject.name + "  clip = " + currentSound);
        
        if (!isPlayed)
        {
            AudioDispatcher.PlayClip(currentSound);
        }

        if (playOncePerActivation)
        {
            isPlayed = true;
        }
    }
}
