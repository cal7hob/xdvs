using System.Collections.Generic;
using UnityEngine;

public class AirMineBOW : MonoBehaviour
{
    private const float STABILIZATION_SPEED = 1.0f;
    private readonly List<AnimationClip> clips = new List<AnimationClip>();
    private new Animation animation;

    void Awake()
    {
        animation = GetComponent<Animation>();
    }

    void OnEnable()
    {
        foreach (AnimationState state in animation)
            clips.Add(state.clip);

        foreach (AnimationClip clip in clips)
            animation.PlayQueued(clip.name);
    }

    void Update()
    {
        if (Vector3.Angle(transform.up, Vector3.up) > 0.01f)
            transform.up = Vector3.MoveTowards(transform.up, Vector3.up, STABILIZATION_SPEED * Time.deltaTime);
    }
}
