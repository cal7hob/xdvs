using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class VehicleSoundItem
{
    public string name = " ";
    public AudioClip clip;
    public float min;
    public float normMin;
    public float normMax;
    public float max;

    public bool usePitch;

    public float pitchMin = 1f;
    public float pitchNormMin = 1f;
    public float pitchNormMax = 1f;
    public float pitchMax = 1f;

    public void UsePitch(bool use)
    {
        usePitch = use;
    }
}