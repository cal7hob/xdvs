using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleController : MonoBehaviour 
{
    /*    protected const float MAX_SYNC_TIME = 0.2f;
    protected const float DEFAULT_SOUND_DISTANCE = 65.0f;

    [Header("Данные")]
    public ObscuredInt id; // TODO: убрать костыльное поле в TankData после поднятия версии комнаты фотона.
    public ObscuredInt tankGroup;
    public TankData data;

    [Header("Префабы")]
    [AssetPathGetter]
    public string terrainHitPrefabPath;
    [AssetPathGetter]
    public string hitPrefabPath;
    [AssetPathGetter]
    public string explosionPrefabPath;
    [AssetPathGetter]
    public string shotPrefabPath;
    [AssetPathGetter]
    public string shellPrefabPath;

    [Header("Ссылки")]
    public Transform lookPoint;
    public Transform cameraEndPoint;
    public Transform forCam;
    public List<Transform> shootEffectPoints = null;

    [Header("Звуки")]
    public AudioClip engineSound;
    public AudioClip turretRotationSound;
    public AudioClip shotSound;
    public AudioClip blowSound;
    public AudioClip explosionSound;
    public AudioClip respawnSound;


    public ObscuredFloat maxSpeed = 5;


    [Header("Прочее")]
    public bool continuousFire;
    public float shotCorrection = 0.3f;
    public AimingController aimingController;
    public TurretController TurretController;
    public Animation shootAnimation;
    public Transform myWeapon;

    [SerializeField]
    private Animator animator;

    [SerializeField]
    private IKController ikController;
    
    [SerializeField]
    public Transform gunDirection;
    //???
    public Transform gunForward;

    [SerializeField]
    private float raycastHaight = 0.5f; // высота на которую отступаем прежде чем кидать луч для определения нахождения на земле
    [SerializeField]
    private float groundedHeight = 0.1f; // высота, на которой считается что мы находимся на земле
    [SerializeField]
    private Transform lookPoint;//точка для прицеливания в нас
    [SerializeField]
    private Transform cameraEndPoint;//конечная точка прицеливания
    #region all about IK
    [SerializeField]
    private float offsetY;
    
    private Transform animRoot;
    private Transform leftFoot;
    private Transform rightFoot;
    private Transform leftHand;
    private Transform rightHand;

    private RaycastHit leftHit;
    private RaycastHit rightHit;

    private float onGroundRayDist;

    public GameObject leftFootObj;
    public GameObject rightFootObj;
    public GameObject leftFootTarget;
    public GameObject rightFootTarget;

    public Vector3 rfPos
    {
        get;
        private set;
    }
    public Vector3 lfPos
    {
        get;
        private set;
    }
    public Quaternion rfRot
    {
        get;
        private set;
    }
    public Quaternion lfRot
    {
        get;
        private set;
    }
    #endregion

    #region Input


    public CruiseControl.CruiseControlState CruiseControlState { get; protected set; }

    public virtual float TurretAxisControl
    {
        get
        {//!BattleGUI.IsWindowOnScreen ?
            return  XDevs.Input.GetAxis("Turret Rotation") ;//: 0
        }
    }

    public virtual float XAxisControl
    {
        get
        {
            return XDevs.Input.GetAxis("Turn Left/Right");
        }
    }

    public virtual float YAxisControl
    {
        get
        {
            return CruiseControlState.YAxisControl();
        }
    }

    protected virtual float XAxisAltControl
    {
        get
        {
            return XDevs.Input.GetAxis("Strafe Left/Right");
        }
    }

    protected virtual float YAxisAltControl
    {
        get
        {
            return XDevs.Input.GetAxis("Turn Up/Down");
        }
    }


    #endregion

    public bool onGround
    {
        get;
        private set;
    }
    

    public void SetLookPoint(Transform lookPoint) 
    {
        this.lookPoint = lookPoint;
    }

	void Start () 
    {
        animator = animator ? animator : GetComponent<Animator>();
        leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        
        rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
 
        ikController = ikController ? ikController : GetComponent<IKController>();

        onGroundRayDist = raycastHaight + offsetY + groundedHeight;
        animRoot = animator.transform;
	}
	

	void Update () 
    {
        FootPositionCheck();
       // Debug.LogFormat("x = {0}  y = {1}", XAxisControl, YAxisControl);
        //MovePlayer();
	}

    private void Gravity() //
    {
        animRoot.Translate(Vector3.down* Time.deltaTime * 10f);
    }
    Vector3 lPos;
    Vector3 rPos;
    private void FootPositionCheck()
    {
        lPos = leftFoot.position;
        rPos = rightFoot.position;

    //    lfRot = leftFoot.rotation;
    //    rfRot = rightFoot.rotation;

        leftFootObj.transform.position = lPos + Vector3.up * raycastHaight;
        rightFootObj.transform.position = rPos + Vector3.up * raycastHaight;

        bool tempOnGround = false;
        Vector3 moveToLPoint;

        if (Physics.Raycast(lPos + Vector3.up * raycastHaight, -Vector3.up, out leftHit, raycastHaight + groundedHeight))//onGroundRayDist
        {
            moveToLPoint = leftHit.point - Vector3.up * offsetY;
            lfRot = Quaternion.FromToRotation(transform.up, leftHit.normal) * transform.rotation;
            tempOnGround = true;
        }
        else 
        {
            moveToLPoint = lPos - Vector3.up * (raycastHaight + groundedHeight);
        }
        lfPos = Vector3.Lerp(lPos, moveToLPoint , Time.deltaTime);
        leftFootTarget.transform.position = lfPos;





        if (Physics.Raycast(rPos + Vector3.up * raycastHaight, -Vector3.up, out rightHit, raycastHaight + groundedHeight))//onGroundRayDist
        {
            rfPos = Vector3.Lerp(rPos, rightHit.point - Vector3.up * offsetY, Time.deltaTime * 10f);
            rightFootTarget.transform.position = rfPos;
            rfRot = Quaternion.FromToRotation(transform.up, rightHit.normal) * transform.rotation;
            tempOnGround = true;
        }
        else 
        {
            rfPos = rPos - Vector3.up * (raycastHaight + groundedHeight);
            rightFootTarget.transform.position = rfPos;
        }
        onGround = tempOnGround;
        
        if (!onGround)
        {
            animRoot.Translate(Vector3.down*5 * Time.deltaTime);
            ikController.ResetParams();
        }
        else 
        {
            //ikController.SetParams(this, animator);
            if (leftHit.distance < raycastHaight || rightHit.distance < raycastHaight) 
            {
                animRoot.Translate(Vector3.up * Time.deltaTime*5);
            }        
          //  Debug.LogFormat("LeftFootPosition = {0}  RightFootPosition = {1}", lfPos, rfPos);
            
        }
    }

    public void MovePlayer()
    {
        transform.position = animRoot.position;
        transform.rotation = animRoot.rotation;
        animRoot.localPosition = Vector3.zero;
        animRoot.localRotation = Quaternion.identity;

       */

        //-------------------------------------------------------
       /* gunDirection.position = gunForward.position;
        gunDirection.LookAt(lookPoint);

        float xAimCos = Vector3.Dot(Vector3.ProjectOnPlane(gunForward.right, gunForward.up).normalized, Vector3.ProjectOnPlane(gunDirection.forward, gunForward.up).normalized);
        if (Vector3.Dot(Vector3.ProjectOnPlane(gunForward.forward, gunForward.up).normalized, Vector3.ProjectOnPlane(gunDirection.forward, gunForward.up).normalized) < 0) 
        {
            xAimCos += xAimCos > 0? 1: -1;
        }

        animator.SetFloat("aimingXAngle", 100 * xAimCos);

        animator.SetFloat("yAxis", YAxisControl);
        animator.SetFloat("xAxis", XAxisControl);

        if (YAxisControl > 0.1 || YAxisControl < -0.1 || XAxisControl > 0.1 || XAxisControl < -0.1) 
        {
            if (xAimCos > 0.5)
            {
                animRoot.Rotate(0, 1, 0, Space.Self);
                //Body.RotateAround
            }
            else if(xAimCos < -0.5)
            {
                animRoot.Rotate(0, -1, 0, Space.Self);
            }
        }
        /*
        curMaxSpeed = maxSpeed * yAxisAcceleration;
        curMaxRotationSpeed = BattleCamera.Instance.IsZoomed ? ZoomRotationSpeed : RotationSpeed;

        if (Mathf.Abs(curMaxSpeed) > MOVEMENT_SPEED_THRESHOLD)
        {
            MarkActivity();
        }

        if (Mathf.Abs(curMaxRotationSpeed) > MOVEMENT_SPEED_THRESHOLD)
        {
            MarkActivity();
        }
    }*/
}
