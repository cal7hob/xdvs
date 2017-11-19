using UnityEngine;
using System.Collections;
using XDevs.LiteralKeys;
using Random = UnityEngine.Random;

public class Map : MonoBehaviour
{
    [SerializeField] private MeshRenderer outlineGrid;
    [SerializeField] private Transform mapCenter;
    [SerializeField] private Collider mapCollider;
    [SerializeField] private float cameraClippingDistance = 1500;
    [SerializeField] private bool isDark;

    private Color outlineGridColor;
    private float offset;   // хз как назвать
    private bool isOutlineGridActivated;
    private IEnumerator outOfMapGridFadingRoutine;
    private Vector3 mapCenterPos;
    private Collider outOfMapWarningCol;
    private Collider outOfMapCol;

    public static Map Instance { get; private set; }
    public static Transform MapCenter { get { return Instance.mapCenter; } }
    public static Vector3 MapCenterPos { get { return Instance.mapCenterPos; } }
    public static Collider OutOfMapWarningCol { get { return Instance.outOfMapWarningCol; } }
    public static Collider OutOfMapCol { get { return Instance.outOfMapCol; } }
    public static bool IsDark { get { return Instance.isDark; } }
    public float CameraClippingDistance { get { return cameraClippingDistance; } }

    void Awake()
    {
        Instance = this;
        outOfMapGridFadingRoutine = OutOfMapGridFader();
        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Subscribe(EventId.BeforeReconnecting, BeforeReconnect);
    }

    void Start()
    {
        SetOutLineGrid();

        var warnColObj = GameObject.FindWithTag(Tag.Items[Tag.Key.OutOfMapWarningCollider]);
        var outMapColObj = GameObject.FindWithTag(Tag.Items[Tag.Key.OutOfMapCollider]);

        outOfMapWarningCol = warnColObj ? warnColObj.GetComponent<Collider>() : null;
        outOfMapCol = outMapColObj ? outMapColObj.GetComponent<Collider>() : null;

        mapCenterPos = outMapColObj ? outMapColObj.transform.position : Vector3.zero;
    }

    void OnDestroy()
    {
        Instance = null;
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Unsubscribe(EventId.BeforeReconnecting, BeforeReconnect);
    }

    public Vector3 FindRandomPointOnMap()
    {
        var rayPos = MapCenter.position + Vector3.forward * Random.Range(mapCollider.bounds.min.z, mapCollider.bounds.max.z) +
                    Vector3.left * Random.Range(mapCollider.bounds.min.x, mapCollider.bounds.max.x) + Vector3.up * (mapCollider.bounds.max.y - 10);

        RaycastHit hitInfo;

        Physics.Raycast(new Ray(rayPos, Vector3.down), out hitInfo);

        return hitInfo.point;
    }

    //private static void LoadAStarPaths()
    //{
    //    var pathToMapPaths = string.Format("{0}/{1}/{2}/{3}", GameManager.CurrentResourcesFolder, "Bots", "MapPaths", SceneManager.GetActiveScene().name);
    //    TextAsset binaryPathsData = Resources.Load<TextAsset>(pathToMapPaths);
    //    if (binaryPathsData != null)
    //    {
    //        AstarPath.active.astarData.DeserializeGraphs(binaryPathsData.bytes);
    //    }
    //}

    private void SetOutLineGrid()
    {
        if (outlineGrid == null)
        {
            return;
        }

        outlineGridColor = outlineGrid.material.color;
        var sqrDistToMapBorder = Mathf.Pow(Mathf.Min(outlineGrid.bounds.extents.x - 80, outlineGrid.bounds.extents.z), 2);
        var minSqrDistToMapBorder = sqrDistToMapBorder * 0.3f;
        offset = sqrDistToMapBorder - minSqrDistToMapBorder;
    }

    private IEnumerator OutOfMapGridFader()
    {
        while (BattleController.MyVehicle != null)
        {
            if (outlineGrid == null)
            {
                yield break;
            }

            var currentDistToCenter =
            Vector3.ProjectOnPlane(outlineGrid.transform.position - BattleController.MyVehicle.transform.position, Vector3.up)
                .sqrMagnitude;

            var alpha = Mathf.Clamp01((currentDistToCenter - offset) / offset);

            if (alpha > 0 && !isOutlineGridActivated)
            {
                isOutlineGridActivated = true;
                SetActiveOutlineGrid(true);
            }
            else if (Mathf.Approximately(alpha, 0) && isOutlineGridActivated)
            {
                isOutlineGridActivated = false;
                SetActiveOutlineGrid(false);
            }

            outlineGridColor.a = alpha;
            outlineGrid.material.color = outlineGridColor;

            yield return new WaitForSeconds(0.1f);
        }
    }

    private void OnMainTankAppeared(EventId id, EventInfo info)
    {
        StartCoroutine(outOfMapGridFadingRoutine);
        StartCoroutine(Settings.SetQuality());
    }

    private void BeforeReconnect(EventId id, EventInfo info)
    {
        StopCoroutine(outOfMapGridFadingRoutine);
    }

    public void SetActiveOutlineGrid(bool activate)
    {
        if (outlineGrid != null)
        {
            outlineGrid.enabled = activate;
        }    
    }
}
