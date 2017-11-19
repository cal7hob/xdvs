using System;
using UnityEngine;

public class ParticleSystemEvents : MonoBehaviour
{
    public static event Action<ParticleSystem> Started = delegate {};

    private new ParticleSystem particleSystem;

    void Awake()
    {
        particleSystem = GetComponent<ParticleSystem>();
    }

    void Start()
    {
        Started(particleSystem);
    }
}
