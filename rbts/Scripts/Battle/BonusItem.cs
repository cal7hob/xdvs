using System;
using ExitGames.Client.Photon;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using Disconnect;
using XDevs.LiteralKeys;

using Hashtable = ExitGames.Client.Photon.Hashtable;

public class BonusItem : MonoBehaviour
{
    public enum BonusType
    {
        None = 0,
        Silver = 2,
        Gold = 3,
        Experience = 4,
        Fuel = 5,
        Reload = 6,
        Boost = 7,
        Attack = 8,
        Landmine = 9,
        Health = 10,

        GoldRush = 50,

        MissileShell = 100,
        ShellEffect = 101,

        Consumable = 200
    }

    [Serializable]
    public class BonusInfo
    {
        public ObscuredInt amount;
        public double appearanceTime;
        public int pointIndex;
        public int ownerId;

        public BonusInfo() { }

        public BonusInfo(int _amount, int _pointIndex, double _appearanceTime, int _ownerId)
        {
            amount = _amount;
            pointIndex = _pointIndex;
            appearanceTime = _appearanceTime;
            ownerId = _ownerId;
        }

        public static byte[] Serialize(object customObject)
        {
            int index = 0;
            BonusInfo info = (BonusInfo)customObject;
            byte[] bytes = new byte[info.pointIndex > 0 ? 20 : 16];
            Protocol.Serialize(info.amount, bytes, ref index);
            BitConverter.GetBytes(info.appearanceTime).CopyTo(bytes, index);
            index += 8;
            Protocol.Serialize(info.ownerId, bytes, ref index);
            if (info.pointIndex > 0)
                Protocol.Serialize(info.pointIndex, bytes, ref index);

            return bytes;
        }

        public static BonusInfo Deserialize(byte[] bytes)
        {
            int index = 0, amount, pointIndex = 0, ownerId;
            double appearanceTime = 0;
            Protocol.Deserialize(out amount, bytes, ref index);
            appearanceTime = BitConverter.ToDouble(bytes, index);

            index += 8;
            Protocol.Deserialize(out ownerId, bytes, ref index);
            if (bytes.Length > 16)
                Protocol.Deserialize(out pointIndex, bytes, ref index);
            BonusInfo info = new BonusInfo(amount, pointIndex, appearanceTime, ownerId);

            return info;
        }
    }

    public BonusType bonusType;
    public BonusInfo info;
    public bool syncByMaster;
    public float rotationSpeed = 60;
    public float levitationHeight = 1;

    public Transform shadow;
    public GameObject effect;
    public float shadowCorrectHeight = 0.05f;
    public LayerMask terrainMask = 0;
    public VehicleEffectData tankEffect;

    private bool isMapBonus;
    private bool isTaken;
    private PhotonView photonView;
    private Renderer[] renderers;

    public bool IsMapBonus
    {
        get { return isMapBonus; }
    }

    public bool IsTaken
    {
        get { return isTaken; }
        set
        {
            isTaken = value;
            SetVisible(!isTaken);
        }
    }

    public int Id { get { return photonView.viewID; } }

    public PhotonView GetPhotonView()
    {
        return photonView;
    }

    void OnPhotonInstantiate(PhotonMessageInfo messageInfo)
    {
        photonView = GetComponent<PhotonView>();
        renderers = GetComponentsInChildren<Renderer>();
        info = (BonusInfo)photonView.instantiationData[0];
        BonusDispatcher.RegisterBonusItem(this);
        if (info.pointIndex > 0)
            BonusPoints.LockPoint(info.pointIndex);
        PhotonView pv = GetComponent<PhotonView>();
        name = "Bonus_" + pv.viewID;
        transform.SetParent(BonusDispatcher.Instance.transform);
        SetHeight();
        SetShadow();
        MapBonus mb = GetComponent<MapBonus>();
        isMapBonus = (mb != null);

        if ((!BattleController.MyVehicle || info.appearanceTime < BattleController.MyCreationTime) && !isMapBonus)
        {
            SetVisible(false);
            return;
        }

        if (isMapBonus)
            mb.Show();
    }

    void OnDestroy()
    {
        if (info.pointIndex > 0)
            BonusPoints.UnlockPoint(info.pointIndex);
        BonusDispatcher.UnRegisterBonusItem(this);
        if (PhotonNetwork.isMasterClient && PhotonNetwork.connected)
        {
            if (isMapBonus)
            {
                PhotonNetwork.room.SetCustomProperties(new Hashtable
                {
                    {"bc", (int) PhotonNetwork.room.CustomProperties["bc"] - 1}
                });
            }
            Messenger.Send(EventId.BonusDestroyed, new EventInfo_II((int)bonusType, photonView.viewID));
        }
    }

    void Update()
    {
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }

    void OnTriggerEnter(Collider other)
    {
        int otherLayer = other.gameObject.layer;
        if (otherLayer == BattleController.ParallelWorldLayer
            || (otherLayer != BattleController.PlayerLayer && !PhotonNetwork.isMasterClient))
            return;

        VehicleController veh = other.transform.GetComponentInParent<VehicleController>();
        if (veh == null || !veh.IsAvailable || !veh.PhotonView.isMine)
            return;

        if (tankEffect.ParameterType != VehicleEffect.ParameterType.None) //Бонус с моментальным эффектом
        {
            veh.RequestEffect(tankEffect);
        }

        Messenger.Send(
            id: EventId.TryingTakeItem,
            info: new EventInfo_II(photonView.viewID, veh.data.playerId),
            target: Messenger.EventTargetType.ToAll);
    }

    public void SetVisible(bool visible)
    {
        gameObject.layer = visible ? LayerMask.NameToLayer(Layer.Items[Layer.Key.Bonus]) : BattleController.ParallelWorldLayer;
        effect.SetActive(visible);
        if (shadow)
            shadow.gameObject.SetActive(visible && QualitySettings.GetQualityLevel() == 0);
        foreach (var rend in renderers)
            rend.enabled = visible;
    }

    private void SetHeight()
    {
        if (terrainMask == 0)
            return;

        RaycastHit hitInfo;
        if (!Physics.Raycast(transform.position, Vector3.down, out hitInfo, 200, terrainMask.value))
        {
            if (PhotonNetwork.isMasterClient)
                PhotonNetwork.Destroy(gameObject);
            return;
        }

        transform.position = hitInfo.point + Vector3.up * levitationHeight;
    }

    private void SetShadow()
    {
        if (!shadow)
            return;

        if (QualitySettings.GetQualityLevel() > 2)
        {
            shadow.gameObject.SetActive(false);
            return;
        }

        RaycastHit hit;
        Vector3 start = shadow.parent.position + Vector3.up;
        Vector3 end = shadow.parent.position + Vector3.down;
        if (Physics.Linecast(start, end, out hit, terrainMask.value))
        {
            //Debug.DrawLine(start, hit.point);
            shadow.position = hit.point + Vector3.up * shadowCorrectHeight;
            // Set(hit.point.x, hit.point.y + correctHeight, hit.point.z);
            shadow.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            shadow.Rotate(Vector3.up, shadow.parent.eulerAngles.y);
        }
    }
}