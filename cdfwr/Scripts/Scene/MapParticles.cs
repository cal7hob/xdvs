using UnityEngine;

public class MapParticles : MonoBehaviour
{
    [SerializeField] private GameObject groundDust; //todo: убрать, когда все будет заменено на groundDustPrefabPath
    [SerializeField] private ParticleSystem groundDustAR;
    [SerializeField] private Color cloudsColor;
    [SerializeField] private ParticleSystemWrapper waterTrail;
    [SerializeField] private GameObject[] cameraParticles;

    [SerializeField, AssetPathGetter] private string groundDustPrefabPath;

    public static MapParticles Instance { get; private set; }  

    public GameObject GroundDust { get { return groundDust; } }

    public string GroundDustPrefabPath { get { return groundDustPrefabPath; } }

    public ParticleSystem GroundDustAR { get { return groundDustAR; } }

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
