using System.Collections;
using UnityEngine;

public class PostFXFlash : MonoBehaviour
{
    private const float MIN_INTENSITY = 0f;
    private const float MAX_INTENSITY = 1f;

    [SerializeField] private float fadeInTime = 1f;
    [SerializeField] private float fadeOutTime = 2f;


    private Material material;
    private int intensityId;
    private float intensity;
    private Coroutine fadingRoutine;
    private ResourceRequest texRequest;
    private Texture2D fxTexture;
    
	void Start()
	{
	    enabled = false;
        material = new Material (Shader.Find("Hidden/White"));
	    intensityId = Shader.PropertyToID("_Intensity");
        Messenger.Subscribe(EventId.IsMainCameraSighted, OnMainCameraSightedSignal);
    }

    void Update()
    {
        material.SetFloat(intensityId, intensity);
    }

    void OnDestroy()
    {
        Resources.UnloadAsset(fxTexture);
        Messenger.Unsubscribe(EventId.IsMainCameraSighted, OnMainCameraSightedSignal);
    }

    void OnRenderImage(RenderTexture source, RenderTexture dest)
    {
		if (Mathf.Approximately(intensity, 0f))
        {
			Graphics.Blit (source,dest);
			return;
		}

		Graphics.Blit (source, dest, material);
	}

    private void FadeIn()
    {
        enabled = true;
        StopExcess();
        if (fxTexture == null)
        {
            texRequest =
                Resources.LoadAsync<Texture2D>(string.Format("{0}/Textures/SkullFX", GameManager.CurrentResourcesFolder));
        }

        fadingRoutine = StartCoroutine(FadeCoroutine(true, fadeInTime));
    }

    private void FadeOut()
    {
        StopExcess();
        fadingRoutine = StartCoroutine(FadeCoroutine(false, fadeOutTime));
    }

    private void StopExcess()
    {
        if (fadingRoutine != null)
        {
            StopCoroutine(fadingRoutine);
            fadingRoutine = null;
        }
    }

    private IEnumerator FadeCoroutine(bool fadingIn, float time)
    {
        // Звуковое сопровождение воспроизвести здесь же

        float targetIntensity = fadingIn ? MAX_INTENSITY : MIN_INTENSITY;
        float speed = Mathf.Abs(targetIntensity - intensity) / time;
        while (!Mathf.Approximately(intensity, targetIntensity))
        {
            intensity = Mathf.MoveTowards(intensity, targetIntensity, speed * Time.deltaTime);
            material.SetFloat(intensityId, intensity);
            if (texRequest != null && texRequest.isDone)
            {
                fxTexture = (Texture2D) texRequest.asset;
                material.SetTexture("_Skull", fxTexture);
                texRequest = null;
            }

            yield return null;
        }

        if (!fadingIn)
        {
            enabled = false;
        }
    }

    private void OnMainCameraSightedSignal(EventId eid, EventInfo ei)
    {
        EventInfo_B info = (EventInfo_B)ei;
        if (info.bool1)
        {
            FadeOut();
        }
        else
        {
            FadeIn();
        }
    }
}
