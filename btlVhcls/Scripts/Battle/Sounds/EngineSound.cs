using UnityEngine;
using System.Collections;

public class EngineSound : SoundControllerBase {

    //Setting's Values
    public float[] MinRpmTable =    {500,   750,   1120,  1669, 2224};
    public float[] NormalRpmTable = {720,   930,   1559,  2028, 2670};
    public float[] MaxRpmTable =    {920,   1360,  1829,  2474, 2943};
    public float[] PitchingTable =  {0.12f, 0.11f, 0.10f, 0.8f, 0.6f};
    //public float[] MinRpmTable =    {500,  720,  1559};
    //public float[] NormalRpmTable = {720,  1559, 2670};
    //public float[] MaxRpmTable =    {1559, 2670, 3000};
    //public float[] PitchingTable = {0.12f, 0.10f, 0.08f};
    //public float[] MinRpmTable = {500, 750, 1120, 1669, 2224, 2783, 3335, 3882, 4355, 4833, 5384, 5943, 6436, 6928, 7419, 7900};
    //public float[] NormalRpmTable = {720, 930, 1559, 2028, 2670, 3145, 3774, 4239, 4721, 5194, 5823, 6313, 6808, 7294, 7788, 8261};
    //public float[] MaxRpmTable = {920, 1360, 1829, 2474, 2943, 3575, 4036, 4525, 4993, 5625, 6123, 6616, 7088, 7589, 8060, 10000};
    //public float[] PitchingTable = {0.12f, 0.12f, 0.12f, 0.12f, 0.11f, 0.10f, 0.09f, 0.08f, 0.06f, 0.06f, 0.06f, 0.06f, 0.06f, 0.06f, 0.06f, 0.06f};

    //Make Values
    //public float Pitching = 0.2f;
    public float RangeDivider = 4f;

    //Make Components
    [Range(520, 2943)]
    public float RPM = 550f;
    //public bool Mode = false;

    public AudioClip[] clips;

    AudioWrapper[] audioSources;

    float rpmQ = 0f;
    float minRPM;
    float maxRPM;
    public float Pitch {
        get { return rpmQ; }
        set {
            rpmQ = value;
            SetRpmQualifier (rpmQ);
        }
    }

    public void SetRpmQualifier (float speed) {
        RPM = minRPM + Mathf.Clamp01 ((speed - .4f) / 1.3f) * maxRPM;
    }

    bool m_isItitiated = false;
    void Start () {
        audioSources = new AudioWrapper[clips.Length];
        minRPM = NormalRpmTable[0];
        maxRPM = NormalRpmTable[NormalRpmTable.Length - 1] - minRPM;

        for (int i = 0; i < clips.Length; i++) {
            audioSources[i] = SetAudioSource (clips[i], true, 0f);
        }
        m_isItitiated = true;
        Play ();
    }

    public void Play () {
        if (!m_isItitiated) return;
        for (int i = 0; i < audioSources.Length; i++) {
            audioSources[i].Play ();
        }
    }

    public void Stop () {
        if (!m_isItitiated) return;
        for (int i = 0; i < audioSources.Length; i++) {
            audioSources[i].Stop ();
        }
    }

    void Update () {
        //Set Volume By Rpm's
        for (int i = 0; i < MinRpmTable.Length; i++) {
            if (RPM < MinRpmTable[i]) {
                audioSources[i].Volume = 0.0f;
            }
            else if (RPM >= MinRpmTable[i] && RPM < NormalRpmTable[i]) {
                float Range = NormalRpmTable[i] - MinRpmTable[i];
                float ReducedRPM = RPM - MinRpmTable[i];
                audioSources[i].Volume = ReducedRPM / Range;
                float PitchMath = (ReducedRPM * PitchingTable[i]) / Range;
                audioSources[i].Pitch = 1f - PitchingTable[i] + PitchMath;
            }
            else if (RPM >= NormalRpmTable[i] && RPM <= MaxRpmTable[i]) {
                float Range = MaxRpmTable[i] - NormalRpmTable[i];
                float ReducedRPM = RPM - NormalRpmTable[i];
                audioSources[i].Volume = 1f;
                float PitchMath = (ReducedRPM * PitchingTable[i]) / Range;
                audioSources[i].Pitch = 1f + PitchMath;
            }
            else if (RPM > MaxRpmTable[i]) {
                float Range = (MaxRpmTable[i + 1] - MaxRpmTable[i]) / RangeDivider;
                float ReducedRPM = RPM - MaxRpmTable[i];
                audioSources[i].Volume = 1f - ReducedRPM / Range;
                //float PitchMath = (ReducedRPM * PitchingTable[i]) / Range;
                //audioSources[i].pitch = 1f + PitchingTable[i] + PitchMath;
            }
        }
    }

}