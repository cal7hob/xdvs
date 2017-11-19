using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[System.Serializable]
public class SoundVehicleBlender : SoundControllerBase
{
    private bool usePitch_ = false;
    public bool usePitch = false;

    public Color moveForwardColor;
    public Color idleColor;
    public Color moveBackwardColor;

    public List<VehicleSoundItem> moveForwardSounds = new List<VehicleSoundItem>();
    public VehicleSoundItem idleSound = new VehicleSoundItem();
    public List<VehicleSoundItem> moveBackwardSounds = new List<VehicleSoundItem>();

    private List<VehicleSoundItem> allSounds = new List<VehicleSoundItem>();
    private AudioWrapper[] audioSources;


    void Start()
    {
        allSounds.AddRange(moveForwardSounds);
        allSounds.Add(idleSound);
        allSounds.AddRange(moveBackwardSounds);
        Debug.Log("Start EngineSoundSwitcher");
        audioSources = new AudioWrapper[allSounds.Count];//[moveForwardSounds.Count+1+moveBackwardSounds.Count];
        for (int i = 0; i < allSounds.Count; i++)
        {
            audioSources[i] = SetAudioSource(allSounds[i].clip, true, 0f);
        }
        /*for (int i = 0; i < moveForwardSounds.Count; i++)
        {
            audioSources[i] = SetAudioSource(moveForwardSounds[i].clip, true, 0f);
        }
        audioSources[moveForwardSounds.Count] = SetAudioSource(idleSound.clip, true, 0f);
        for (int i = 0; i < moveBackwardSounds.Count ; i++)
        {
            audioSources[i + moveForwardSounds.Count + 1] = SetAudioSource(moveBackwardSounds[i].clip, true, 0f);
        }*/
        Play();
    }
    float speed;
    public void SetRpmQualifier(float speed)
    {
        this.speed = speed;
        
    }

    public void Play()
    {
        for (int i = 0; i < audioSources.Length; i++)
        {
            audioSources[i].Play();
        }
    }

    public void Stop()
    {
        for (int i = 0; i < audioSources.Length; i++)
        {
            audioSources[i].Stop();
        }
    }

    void Update()
    {
        for (int i = 0; i < allSounds.Count; i++) 
        {
            VehicleSoundItem clip_ = allSounds[i];
            if (speed < clip_.min || speed > clip_.max)
            {
                audioSources[i].Volume = 0.0f;
            }
            else 
            {
                float normalizeSound = 1f;
                if (speed >= clip_.normMin && speed <= clip_.normMax)
                {
                    normalizeSound = 1f;
                }
                else 
                {
                    if (speed < clip_.normMin) //   /
                    {
                        normalizeSound = (speed - clip_.min) / (clip_.normMin - clip_.min);
                    }
                    else//  \
                    {
                        normalizeSound = (clip_.max - speed)/(clip_.max - clip_.normMax);
                    }
                }

                audioSources[i].Volume = normalizeSound;
            }
        }

          /*  for (int i = 0; i < MinSpeedTable.Length; i++)
            {
                float Range;
                float ReducedSpeed;
                if (speed < MinSpeedTable[i])
                {
                    if (speed > 0.1f)// && speed < 1.1f)
                    {
                        Debug.Log("disable " + i + "   " + MinSpeedTable[i]);
                    }
                    audioSources[i].Volume = 0.0f;
                }
                else if (speed >= MinSpeedTable[i] && speed < NormalSpeedTable[i])
                {
                    Range = NormalSpeedTable[i] - MinSpeedTable[i];
                    ReducedSpeed = speed - MinSpeedTable[i];

                    audioSources[i].Volume = Mathf.Abs(ReducedSpeed / Range);

                    float PitchMath = (ReducedSpeed * testPitchingTable[i]) / Range;
                    audioSources[i].Pitch = 1f - testPitchingTable[i] + PitchMath;
                    Debug.Log(i + " " + audioSources[i].Pitch);
                }
                else if (speed >= NormalSpeedTable[i] && speed <= MaxSpeedTable[i])
                {
                    Range = MaxSpeedTable[i] - NormalSpeedTable[i];
                    ReducedSpeed = speed - NormalSpeedTable[i];
                    audioSources[i].Volume = 1f;

                    float PitchMath = (ReducedSpeed * testPitchingTable[i]) / Range;
                    audioSources[i].Pitch = 1f + PitchMath;
                }
                else if (speed > MaxSpeedTable[i])
                {
                    Range = (MaxSpeedTable[i + 1] - MaxSpeedTable[i]) / RangeDivider;
                    ReducedSpeed = speed - MaxSpeedTable[i];
                    audioSources[i].Volume = Mathf.Abs(1f - ReducedSpeed / Range);
                    //  float PitchMath = (ReducedSpeed * PitchingTable[i]) / Range;
                    //  audioSources[i].Pitch = 1f + PitchingTable[i] + PitchMath;
                }
            }*/

        
    }
}
