using UnityEngine;

public class MapParticles : MonoBehaviour
{
    public GameObject groundDust;
    public ParticleSystem groundDustAR;
    public GameObject cloudsPrefab;
    public Color cloudsColor;
    public ParticleSystemWrapper waterTrail;
    public GameObject[] cameraParticles;

    public static MapParticles Instance { get; private set; }

    public GameObject GroundDust { get { return groundDust; } }

    public ParticleSystem GroundDustAR { get { return groundDustAR; } }

    public Color CloudsColor { get { return cloudsColor; } }

    public ParticleSystemWrapper WaterTrail { get { return waterTrail; } }

    public GameObject[] CameraParticles { get { return cameraParticles; } }

    void Awake()
    {
        Instance = this;
        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
    }

    void OnDestroy()
    {
        Instance = null;
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
    }

    private void OnMainTankAppeared(EventId id, EventInfo ei)
    {
        if (cloudsPrefab == null || QualitySettings.GetQualityLevel() <= 1)
            return;

        GameObject clouds = Instantiate(cloudsPrefab);

        var main = clouds.GetComponentInChildren<ParticleSystem>().main;
        main.startColor = Instance.CloudsColor;

        clouds.transform.parent = BattleController.MyVehicle.transform.Find("Body/Effects");

        clouds.transform.localPosition = Vector3.zero;
        clouds.transform.rotation = Quaternion.identity;
    }
}
