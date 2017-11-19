using UnityEngine;
using XDevs.LiteralKeys;

public class AudioObstacleCutoff : MonoBehaviour
{
    public float speed = 1.5f;

    [Range(0, INITIAL_FREQUENCY)]
    public float minFrequency = 4186.0f;

    private const float INITIAL_FREQUENCY = 21990.0f;
    private const float INSTANT_CUTOFF_CLIP_LENGTH = 4.0f;
    
    private AudioSource source;
    private AudioLowPassFilter filter;
    private int layerMask;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        filter = GetComponent<AudioLowPassFilter>();

        if (filter == null)
            filter = gameObject.AddComponent<AudioLowPassFilter>();

        layerMask
            = MiscTools.GetLayerMask(
                Layer.Key.Default,
                Layer.Key.Terrain);
    }

    void Update()
    {
        SetCutoff();
    }

    private void SetCutoff()
    {
        if (!source.isPlaying)
            return;

        float listenerDistance = Vector3.Distance(transform.position, Camera.main.transform.position);
        Vector3 listenerDirection = (Camera.main.transform.position - transform.position).normalized;

        bool isOverlapped
            = Physics.Raycast(
                origin:         transform.position,
                direction:      listenerDirection,
                maxDistance:    listenerDistance,
                layerMask:      layerMask);

        if (source.clip.length < INSTANT_CUTOFF_CLIP_LENGTH)
            InstantCutoff(isOverlapped);
        else
            SmoothCutoff(isOverlapped);
    }

    private void InstantCutoff(bool overlapped)
    {
        filter.cutoffFrequency = overlapped ? minFrequency : INITIAL_FREQUENCY;
    }

    private void SmoothCutoff(bool overlapped)
    {
        filter.cutoffFrequency
            = Mathf.MoveTowards(
                current:    filter.cutoffFrequency,
                target:     overlapped ? minFrequency : INITIAL_FREQUENCY,
                maxDelta:   speed * INITIAL_FREQUENCY * Time.deltaTime);
    }
}
