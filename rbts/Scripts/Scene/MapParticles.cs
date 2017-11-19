using UnityEngine;

public class MapParticles : MonoBehaviour
{
    public string groundDust;
    public Color cloudsColor;
    public GameObject[] cameraParticles;

    public static MapParticles Instance { get; private set; }

    public string GroundDust { get { return groundDust; } }

    public Color CloudsColor { get { return cloudsColor; } }

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
