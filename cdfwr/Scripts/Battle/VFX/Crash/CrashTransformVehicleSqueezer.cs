using UnityEngine;
using System.Collections;

public class CrashTransformVehicleSqueezer : MonoBehaviour
{
    public Transform turret;
    [Header("Процент сжатия в единицу игрового времени")]
    [SerializeField]
    private float compressPersent_ = 2f;
    [Header("Задержка в сек. перед сжатием crash модели")]
    [SerializeField]
    private float delay_ = 2f;

    private int playerId;
    private bool isFirstUse = true;
    private Vector3 startLocalPos;// For Body,Turret and maybe Cannon
    private float compressStepBody;
    private float compressStepTurret;
    private float compressStepCannon;
    [SerializeField]
    private Renderer body_;
    [SerializeField]
    private Renderer turret_;
    [SerializeField]
    private Renderer cannon_;

    void Awake()
    {
        Dispatcher.Subscribe(EventId.TankLeftTheGame, OnTankLeftTheGame);
    }

    void OnEnable() 
    {
        if (isFirstUse)
        {
            compressStepBody = body_.bounds.size.y * compressPersent_ * 0.01f;
            compressStepTurret = turret_.bounds.size.y * compressPersent_ * 0.01f;
            if (cannon_ != null)
            {
                compressStepCannon = cannon_.bounds.size.y * compressPersent_ * 0.01f;
            }
            startLocalPos = new Vector3(body_.transform.localPosition.y, turret_.transform.localPosition.y, cannon_== null? 0:cannon_.transform.localPosition.y);
            isFirstUse = false;
        }

        StartCoroutine("Squeezing");
    }

    void OnDisable() 
    {
        body_.transform.localPosition = new Vector3(body_.transform.localPosition.x, startLocalPos.x, body_.transform.localPosition.z);//startYSize.Set(body_.bounds.size.y, turret_.bounds.size.y);
        body_.transform.localScale = Vector3.one;

        turret_.transform.localPosition = new Vector3(turret_.transform.localPosition.x, startLocalPos.y, turret_.transform.localPosition.z);//startYSize.Set(body_.bounds.size.y, turret_.bounds.size.y);
        turret_.transform.localScale = Vector3.one;
        
        if (cannon_ != null) 
        {
            cannon_.transform.localPosition = new Vector3(cannon_.transform.localPosition.x, startLocalPos.z, cannon_.transform.localPosition.z);//startYSize.Set(body_.bounds.size.y, turret_.bounds.size.y);
            cannon_.transform.localScale = Vector3.one;
        }
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TankLeftTheGame, OnTankLeftTheGame);
    }

    public void Init(VehicleController vehicleController)
    {
        playerId = vehicleController.data.playerId;
    }

    private void OnTankLeftTheGame(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;

        int playerId = info.int1;

        if (playerId == this.playerId)
        {
            Destroy(gameObject);
        }
    }


    public IEnumerator Squeezing()
    {
        yield return new WaitForSeconds(delay_);
        float scale_ = compressPersent_ * 0.01f;
        while (body_.transform.localScale.y > 0.1f)
        {
            body_.gameObject.transform.localScale -= new Vector3(0, scale_, 0);
            turret_.gameObject.transform.localScale -= new Vector3(0, scale_, 0);
            
            body_.transform.localPosition -= new Vector3(0, compressStepBody * 0.5f, 0);
            turret_.transform.localPosition -= new Vector3(0, compressStepBody * 0.5f + compressStepTurret * 0.5f, 0);

            if (cannon_ != null) 
            {
                cannon_.gameObject.transform.localScale -= new Vector3(0, scale_, 0);
                cannon_.transform.localPosition -= new Vector3(0, compressStepBody * 0.5f + compressStepTurret * 0.5f + compressStepCannon * 0.5f, 0);
            }

            yield return new WaitForSeconds(0.1f);
        }

        body_.gameObject.SetActive(false);
        turret_.gameObject.SetActive(false);
        cannon_.gameObject.SetActive(false);

    }
}
