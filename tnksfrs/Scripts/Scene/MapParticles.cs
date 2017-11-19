using UnityEngine;

public class MapParticles : MonoBehaviour
{
    public GameObject groundDust;
    public Color cloudsColor;
    public ParticleSystemWrapper waterTrail;
    public GameObject[] cameraParticles;

    public static MapParticles Instance { get; private set; }

    public GameObject GroundDust { get { return groundDust; } }

    public Color CloudsColor { get { return cloudsColor; } }

    public ParticleSystemWrapper WaterTrail { get { return waterTrail; } }

    public GameObject[] CameraParticles { get { return cameraParticles; } }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        Instance = null;
    }
}
