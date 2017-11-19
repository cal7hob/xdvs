using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class VideoOperator : MonoBehaviour
{
//=============================================================================================================================
//camera
//=============================================================================================================================
    public Transform cameraTransform;
    public float moveDamping = 5.0f;
    public float rotationDamping = 2.0f;
    public float zoomSpeed = 2000;
    public float zoomMax = 100;
    public float zoomMin = 10;
    private Camera currentCamera;
    private float zoomCurrent;
    private bool lockCamera = false;
    private float targetRotationAngleX = 0;
    private float targetRotationAngleY = 0;
    private float currentRotationAngleY;
    private float currentRotationAngleX;
    private float zoom;

    public static void UpdateCheckPressKey()
    {
        if (!init && Input.GetKeyDown(KeyCode.Alpha0)) Init();
    }

    public static void Init()
    {
#if UNITY_EDITOR
        if (init) return;
        Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<VideoOperator>(GetDirectory() + "/VideoOperator.prefab"));//videoOperator
#endif
    }

#if UNITY_EDITOR
    public static string GetDirectory()
    {
        string resuslt = Path.GetDirectoryName(new System.Diagnostics.StackTrace(1, true).GetFrame(0).GetFileName().Replace("\\", "/"));
        int index = resuslt.IndexOf("Assets");
        return resuslt.Substring(index, resuslt.Length - index);
    }
#endif

    void LateUpdate()
    {
        currentRotationAngleY = cameraTransform.eulerAngles.y;
        currentRotationAngleX = cameraTransform.eulerAngles.x;

        if (onTrackDirection)
        {
            if (Input.GetKey(KeyCode.Mouse0))
            {
                targetRotationAngleY = Mathf.Repeat(targetRotationAngleY + Input.GetAxis("Mouse X"), 360);
                targetRotationAngleX = Mathf.Clamp(targetRotationAngleX - Input.GetAxis("Mouse Y"), -90, 90);
            }

            if (Input.GetKey(KeyCode.Mouse1))
            {
                targetRotationAngleY = 0;
                targetRotationAngleX = 0;
            }
            currentRotationAngleY = Mathf.LerpAngle(currentRotationAngleY, Mathf.Repeat(targetTransform.eulerAngles.y + targetRotationAngleY, 360), rotationDamping * Time.deltaTime);
            currentRotationAngleX = Mathf.LerpAngle(currentRotationAngleX, targetRotationAngleX, rotationDamping * Time.deltaTime);
        }
        else
        {
            currentRotationAngleY = Mathf.LerpAngle(currentRotationAngleY, targetTransform.eulerAngles.y, rotationDamping * Time.deltaTime);
            if (!lockCamera) currentRotationAngleX = Mathf.LerpAngle(currentRotationAngleX, targetTransform.eulerAngles.x, rotationDamping * Time.deltaTime);
        }

        Quaternion currentRotation = Quaternion.Euler(currentRotationAngleX, currentRotationAngleY, 0);

        cameraTransform.position = Vector3.Lerp(cameraTransform.position, targetTransform.position, moveDamping * Time.deltaTime);
        cameraTransform.rotation = currentRotation;

        zoom = Input.GetAxis("Mouse ScrollWheel");
        if (zoom != 0) zoomCurrent = Mathf.Clamp(zoomCurrent + zoom * zoomSpeed * Time.deltaTime, zoomMin, zoomMax);
        currentCamera.fieldOfView = Mathf.Lerp(currentCamera.fieldOfView, zoomCurrent, rotationDamping * Time.deltaTime);
    }

//=============================================================================================================================
//controll
//=============================================================================================================================
    public float turnSpeed = 40f;
    public float forwardSpeed = 3500f;
    public float turnSpeedTrack = 150f;
    //public float distanceTarget = 0;
    public float minDistanceVertex = 1;
    public TrackName trackName = TrackName.Common;
    public float timeScale = 1;
    private Transform targetTransform;
    private Rigidbody targetRigidbody;
    private static bool init = false;
    private float turnAmountY = 0;
    private float turnAmountX = 0;
    private SphereCollider targetCollider;
    private bool onTrackDirection = false;
    //private DijkstraAlg dijkstraAlg;
    public List<Vertex> track = new List<Vertex>();
    private int selectVertexIndex = 0;
    private float speedMove;
    private float forwardMoveAmount;
    private Vector3 target;
    private float distance;
    private Quaternion targetRotation = new Quaternion();
    private float sideAmount = 0;
    private float upDown = 0;

    void Awake()
    {
        init = true;
        targetTransform = transform;
        targetRigidbody = GetComponent<Rigidbody>();
        targetCollider = GetComponent<SphereCollider>();
        currentCamera = Camera.main;
        zoomCurrent = currentCamera.fieldOfView;
        cameraTransform = currentCamera.transform;

        turnAmountY = cameraTransform.rotation.eulerAngles.y; //targetRigidbody.rotation.eulerAngles.y;
        turnAmountX = targetRigidbody.rotation.eulerAngles.x;

        cameraTransform.GetComponent<SphereCollider>().enabled = false;
        targetRigidbody.MovePosition(cameraTransform.position);
        targetRigidbody.MoveRotation(cameraTransform.rotation);
        cameraTransform.GetComponent<GroundCamera>().enabled = false;
        GameObject.Find("Battle2D_FTRobotsInvasion").SetActive(false);
        SetLayer(BattleController.allVehicles[BattleController.MyPlayerId].transform, LayerMask.NameToLayer("ParallelWorld"));

        //dijkstraAlg = new DijkstraAlg(FeatureType.Common, 0.1f);
    }

    void SetLayer(Transform transform, int layer)
    {
        transform.gameObject.layer = layer;
        foreach (Transform child in transform) SetLayer(child, layer);
    }
    
    void Update()
    {
        speedMove = Time.deltaTime * (Input.GetKey(KeyCode.LeftShift) ? forwardSpeed * 3 : forwardSpeed); //KeyCode.Space
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            onTrackDirection = !onTrackDirection;
            if (onTrackDirection)
            {
                Vertex vertex = WayPoints.instance.vertices[0];
                targetRigidbody.MovePosition(vertex.position);
                GetTrack(vertex);
                //List<Track> tracks = vertex.track[trackName].Values;
                //track = tracks[tracks.Count - 1].track;
                selectVertexIndex = 0;

                //targetRotation.SetLookRotation((NextTrack(targetTransform.position) - targetTransform.position).normalized);
                //targetRigidbody.MoveRotation(targetRotation);
            }
        }
        else
        {
            if (onTrackDirection)
            {
                forwardMoveAmount = 3 * speedMove;
                //Vector3 direction = dijkstraAlg.GetTrack(targetTransform.position, WayPoints.instance.vertices[WayPoints.instance.vertices.Length - 1].position, FeatureType.Common, ref distanceTarget) - targetTransform.position;
                target = NextTrack(targetTransform.position);
                targetRotation.SetLookRotation((target - targetTransform.position).normalized);
                //float angle = Vector3.Angle(targetRotation * Vector3.forward, targetTransform.rotation * Vector3.forward);
                distance = Vector3.Distance(targetTransform.position, target); //Mathf.Pow(Vector3.Distance(targetTransform.position, target), 2);
                //float speed = Vector3.Distance(Vector3.zero, targetRigidbody.velocity) * Time.deltaTime;
                //Debug.Log(angle + " " + speed);
                //targetRigidbody.MoveRotation(Quaternion.Euler(targetTransform.rotation.eulerAngles.x, targetTransform.rotation.eulerAngles.y + angle / ((distance / speed) / 2), targetTransform.rotation.eulerAngles.z));

                targetRigidbody.MoveRotation(Quaternion.Lerp(targetTransform.rotation, targetRotation, Time.deltaTime * ((turnSpeedTrack / (Input.GetKey(KeyCode.LeftShift) ? distance / 10 : distance)))));
                //targetRigidbody.MoveRotation(Quaternion.Lerp(targetTransform.rotation, targetRotation, turnSpeedTrack * (angle / (distance / speed))));
            }
            else
            {
                forwardMoveAmount = Input.GetAxis("VerticalAC") * speedMove;

                if (Input.GetKey(KeyCode.Mouse0))
                {
                    turnAmountY = Mathf.Repeat(targetRigidbody.rotation.eulerAngles.y + Input.GetAxis("Mouse X") * (Time.deltaTime * turnSpeed), 360);
                    turnAmountX = Mathf.Clamp(turnAmountX - Input.GetAxis("Mouse Y"), -90, 90);
                }

                if (Input.GetKey(KeyCode.Mouse1))
                {
                    lockCamera = true;
                    targetRigidbody.MoveRotation(Quaternion.Euler(0, turnAmountY, 0));
                }
                else
                {
                    targetRigidbody.MoveRotation(Quaternion.Euler(turnAmountX, turnAmountY, targetRigidbody.rotation.eulerAngles.z));
                    if (Mathf.Approximately(targetTransform.rotation.eulerAngles.x, turnAmountX)) lockCamera = false;//targetTransform.rotation.eulerAngles.x == turnAmountX
                }

                sideAmount = Input.GetAxis("HorizontalAC") * speedMove;
                upDown = Input.GetAxis("UpDown") * speedMove;
            }
        }
        
        targetRigidbody.AddRelativeForce(sideAmount, upDown, forwardMoveAmount);

        if (Input.GetKeyDown(KeyCode.Mouse2)) Cursor.visible = !Cursor.visible;
        if (Input.GetKeyDown(KeyCode.F)) targetCollider.enabled = !targetCollider.enabled;
        if (Input.GetKeyDown(KeyCode.V) && timeScale <= 100) Time.timeScale = timeScale += 0.2f;
        if (Input.GetKeyDown(KeyCode.C) && timeScale > 0.2f) Time.timeScale = timeScale -= 0.2f;
    }

    private Vector3 NextTrack(Vector3 thisPoint)
    {
        if (track.Count > 2)
        {
            if (Vector3.Distance(thisPoint, track[selectVertexIndex].position) < minDistanceVertex) selectVertexIndex++;
            if (track.Count > selectVertexIndex) return track[selectVertexIndex].position;
        }
        onTrackDirection = false;
        return track[track.Count - 1].position;
    }

    private void GetTrack(Vertex start)
    {
        Vertex select = start;
        Vertex result;
        int index = 0;
        //int indexItem2;
        track.Clear();
        track.Add(select);
        while (index != -1)
        {
            index = select.waysVertexColors.IndexOf(trackName, index);
            if (index == -1) return;
            result = select.waysVertex[index];
            if (!track.Contains(result))
            {
                index = 0;
                select = result;
                track.Add(select);
            }
            else
            {
                index++;
            }
        }
    }
}