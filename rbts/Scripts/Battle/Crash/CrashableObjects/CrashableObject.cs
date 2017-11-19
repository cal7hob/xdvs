using Pool;
using UnityEngine;

public class CrashableObject : MonoBehaviour
{
    protected const float SOUND_VOLUME_RATIO = 1.0f;

    private bool isCrashed;
    public Transform[] crashObjects;
    public AudioClip[] sounds;
    public FXInfo crashEffect;
    public Transform effectPoint;

    private Vector3 beforeCrashPosition;
    private Quaternion beforeCrashRotation;
    private Transform beforeCrashParent;

    protected virtual void CrashItself()
    {
        gameObject.SetActive(false);

        Transform crashObject = null;
        if (crashObjects != null && crashObjects.Length > 0)
        {
            crashObject = crashObjects.GetRandomItem();
        }

        if (crashObject != null)
        {
            beforeCrashPosition = crashObject.position;
            beforeCrashRotation = crashObject.rotation;
            beforeCrashParent = crashObject.parent;
            crashObject.SetParent(transform.parent);
            crashObject.gameObject.SetActive(true);
        }

        if (sounds.Length > 0)
            AudioDispatcher.PlayClipAtPosition(
                clip: sounds.GetRandomItem(),
                position: transform.position,
                channel: AudioPlayer.Channel.Important,
                loop: false,
                parent: null,
                volume: Settings.SoundVolume * SOUND_VOLUME_RATIO);

        Vector3 effectPosition = transform.position;
        Quaternion effectRotation = Quaternion.identity;

        if (effectPoint != null)
        {
            effectPosition = effectPoint.position;
            effectRotation = effectPoint.rotation;
        }

        if (crashEffect != null)
        {
            string fxPath = crashEffect.GetResourcePath(true);
            if (!string.IsNullOrEmpty(fxPath))
            {
                PoolManager.GetObject<PoolEffect>(
                    fxPath,
                    effectPosition,
                    effectRotation
                    );
            }
        }
    }

    protected virtual void RestoreItself()
    {
        gameObject.SetActive(true);

        foreach (Transform crashObject in crashObjects)
        {
            if (crashObject != null)
            {
                crashObject.gameObject.SetActive(false);
                crashObject.SetParent(beforeCrashParent);
                crashObject.localPosition = beforeCrashPosition;
                crashObject.localRotation = beforeCrashRotation;
            }
        }
    }

    public void Crash()
    {
        if (isCrashed)
            return;

        isCrashed = true;

        CrashItself();
    }

    public void Restore()
    {
        isCrashed = false;
        RestoreItself();
    }
}
