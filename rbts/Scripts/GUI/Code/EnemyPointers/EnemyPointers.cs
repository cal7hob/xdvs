using System;
using UnityEngine;
using System.Collections.Generic;
public class EnemyPointers : MonoBehaviour
{
    public class EnemyInfo : IComparable<EnemyInfo>
    {
        public readonly EnemyPointer pointer;

        public EnemyInfo(EnemyPointer pointer)
        {
            this.pointer = pointer;

            SqrDistance = 0;

            pointer.info = this;
        }

        public float SqrDistance
        {
            get; private set;
        }

        public int CompareTo(EnemyInfo other)
        {
            return (int)Mathf.Sign(SqrDistance - other.SqrDistance);
        }

        public void RefreshDistance()
        {
            SqrDistance = Vector3.SqrMagnitude(BattleController.MyVehicle.transform.position - pointer.Vehicle.transform.position);
        }
    }

    public EnemyPointer pointerPrefab;

    private const int ENEMIES_IN_VIEW = 5;
    private const float ALPHA_RATIO = 0.1f;

    private readonly LinkedList<EnemyInfo> enemies = new LinkedList<EnemyInfo>();
    private readonly MiscTools.OrderedLinkedList<EnemyInfo> orderedEnemies = new MiscTools.OrderedLinkedList<EnemyInfo>();
    private readonly Dictionary<int, EnemyPointer> pointers = new Dictionary<int, EnemyPointer>(GameData.maxPlayers);

    [SerializeField]
    private Vector2 fieldSize;

    private Transform cameraTransform;
    private float viewAngleCos;
    

    public static Vector2 FieldSize
    {
        get; private set;
    }

    void Awake()
    {
        FieldSize = fieldSize;
        cameraTransform = Camera.main.transform;
        viewAngleCos = Mathf.Cos(Mathf.Deg2Rad * Camera.main.fieldOfView * 0.5f);
        
        Messenger.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Messenger.Subscribe(EventId.TankJoinedBattle, OnTankConnected);
        Messenger.Subscribe(EventId.TankAvailabilityChanged, OnTankAvailabilityChanged);
        Messenger.Subscribe(EventId.TankLeftTheGame, OnTankLeftTheGame);
        Messenger.Subscribe(EventId.SecondaryWeaponUsed, OnSecondaryWeaponUsed);

        this.InvokeRepeating(SelectEnemies, 0, 0.2f);
    }

    void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Messenger.Unsubscribe(EventId.TankJoinedBattle, OnTankConnected);
        Messenger.Unsubscribe(EventId.TankAvailabilityChanged, OnTankAvailabilityChanged);
        Messenger.Unsubscribe(EventId.TankLeftTheGame, OnTankLeftTheGame);
        Messenger.Unsubscribe(EventId.SecondaryWeaponUsed, OnSecondaryWeaponUsed);
    }

    private void OnMainTankAppeared(EventId id, EventInfo ei)
    {
        EnablePoints();
    }

    private void OnTankAvailabilityChanged(EventId eid, EventInfo ei)
    {
        EventInfo_II info = (EventInfo_II)ei;

        EnemyPointer pointer;

        if (!pointers.TryGetValue(info.int1, out pointer))
            return;

        if (BattleController.MyVehicle && info.int2 == 0)
            pointer.gameObject.SetActive(false);
    }

    private void OnTankConnected(EventId eid, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;

        VehicleController connected;

        if (!BattleController.allVehicles.TryGetValue(info.int1, out connected) || connected.IsMain || connected.IsMainsFriend)
            return;

        EnemyPointer pointer = Instantiate(pointerPrefab);

        pointer.transform.SetParent(transform);
        pointer.Vehicle = connected;
        pointer.gameObject.SetActive(false);

        pointers.Add(info.int1, pointer);

        enemies.AddLast(new EnemyInfo(pointer));

        pointer.gameObject.SetActive(BattleController.MyVehicle && connected.IsAvailable);
    }

    private void OnTankLeftTheGame(EventId eid, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;

        EnemyPointer pointer;

        if (!pointers.TryGetValue(info.int1, out pointer))
            return;

        enemies.Remove(pointer.info);
        pointers.Remove(info.int1);

        Destroy(pointer.gameObject);
    }

    private void OnSecondaryWeaponUsed(EventId eid, EventInfo ei)
    {
        EventInfo_III info = (EventInfo_III)ei;

        if (info.int1 != BattleController.MyPlayerId && info.int3 != BattleController.MyPlayerId)
            return;

        EnemyPointer pointer;

        if (!pointers.TryGetValue(info.int1, out pointer) && !pointers.TryGetValue(info.int3, out pointer))
            return;

        foreach (var enemyPointer in pointers)
            enemyPointer.Value.IsMain = false;

        pointer.IsMain = true;
    }

    private void EnablePoints()
    {
        foreach (EnemyPointer pointer in pointers.Values)
            pointer.gameObject.SetActive(true);
    }

    private bool EnemyInView(Transform enemyTransform)
    {
        return Vector3.Dot(cameraTransform.forward, (enemyTransform.position - cameraTransform.position).normalized) >= viewAngleCos;
    }

    private void SelectEnemies()
    {
        if (!BattleController.MyVehicle)
            return;

        orderedEnemies.Clear();

        foreach (var enemy in enemies)
        {
            if (enemy.pointer.Vehicle.IsAvailable)
                orderedEnemies.Add(enemy);
        }

        int n = 0;
        int pointerIndex = 0;

        foreach (EnemyInfo info in orderedEnemies)
        {
            n++;
            bool flag = (n <= ENEMIES_IN_VIEW) && !EnemyInView(info.pointer.Vehicle.transform);
            if (flag)
            {
                info.pointer.Alpha = 1 - pointerIndex * ALPHA_RATIO;
                pointerIndex++;
            }

            GameObject pointerGO = info.pointer.gameObject;

            if (pointerGO.activeSelf != flag)
                pointerGO.SetActive(flag);
        }
    }
}
