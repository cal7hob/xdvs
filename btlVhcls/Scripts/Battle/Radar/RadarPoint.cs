using UnityEngine;

public class RadarPoint : MonoBehaviour
{
    public tk2dSprite mainPoint;
    public tk2dBaseSprite[] additionalSpritesToScale;
    public GameObject chatMessageWrapper;

    private const float MAIN_ENEMY_SCALE_MULTIPLIER = 1.5f;

    private bool isMain;
    private float sqrMaxPointDistance;
    private Vector3 regularScale;
    private BattleChatPanelItemData chatMessageData;

    public bool IsMain
    {
        get
        {
            return isMain;
        }
        set
        {
            isMain = value;

            if (isMain)
            {
                Vector3 newScale = regularScale * MAIN_ENEMY_SCALE_MULTIPLIER;
                Vector3 sprScale = new Vector3(newScale.x, newScale.y, mainPoint.scale.z);
                mainPoint.scale = sprScale;
                HelpTools.SetScaleToAllSpritesInCollection(additionalSpritesToScale, sprScale);
            }
            else
            {
                mainPoint.scale = regularScale;
                HelpTools.SetScaleToAllSpritesInCollection(additionalSpritesToScale, regularScale);//���� ����� � �������� ����� ���������� - �������� ������ ������� ��������� �������
            }
                
        }
    }

    public VehicleController Target
    {
        get
        {
            return target;
        }
        set
        {
            if (!value)
            {
                Debug.Log("<color=\"red\">NULL as target disables Radar point</color>", gameObject);

                gameObject.SetActive(false);

                return;
            }

            target = value;

            Colorize();

            if (BattleController.MyVehicle)
                Redraw();
        }
    }

    private VehicleController target;

    void Awake()
    {
        var maxPointDistance = RadarController.MaxPointDistance - (mainPoint.GetBounds().extents.x / 2);

        sqrMaxPointDistance = maxPointDistance * maxPointDistance;

        regularScale = mainPoint.scale;

        if (chatMessageWrapper)
            chatMessageWrapper.SetActive(false);

        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankExists);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankExists);
    }
    
    void Update()
    {
        if (!target || !BattleController.MyVehicle)
            return;

        Redraw();

        #region ���������� ����� ����
        if (chatMessageWrapper && chatMessageWrapper.activeSelf && chatMessageData != null && !chatMessageData.IsLive)
            chatMessageWrapper.SetActive(false);
        #endregion
    }

    private void OnMainTankExists(EventId id, EventInfo ei)
    {
        Colorize();
    }

    private void Redraw()
    {
        if (GameData.IsGame(Game.BattleOfWarplanes | Game.WingsOfWar | Game.BattleOfHelicopters))
        {
            RedrawForBOW();
            return;
        }

        Vector3 targetPos = BattleController.MyVehicle.transform.InverseTransformPoint(target.transform.position);

        transform.localPosition = RadarController.DisplayRatio * new Vector3(targetPos.x, targetPos.z, 0);

        if (Vector3.SqrMagnitude(Vector3.zero - transform.localPosition) > sqrMaxPointDistance)
        {
            if (mainPoint.gameObject.activeSelf)
                mainPoint.gameObject.SetActive(false);

            return;
        }

        if (!mainPoint.gameObject.activeSelf)
            mainPoint.gameObject.SetActive(true);
    }

    private void RedrawForBOW()
    {
        Vector3 targetPos = BattleController.MyVehicle.transform.InverseTransformPoint(target.transform.position);

        transform.localPosition = RadarController.DisplayRatio * new Vector3(targetPos.x, targetPos.z, 0);

        if (!mainPoint.gameObject.activeSelf)
            mainPoint.gameObject.SetActive(true);

        if (Vector3.SqrMagnitude(Vector3.zero - transform.localPosition) > sqrMaxPointDistance)
            transform.localPosition = transform.localPosition.normalized * Mathf.Sqrt(sqrMaxPointDistance);
    }

    private void Colorize()
    {
        mainPoint.color = target.IsMainsFriend ? Color.green : Color.red;
    }

    public void SetupChatMessage(BattleChatPanelItemData data)
    {
        if (data == null || target == null)
            return;
        chatMessageData = data;
        chatMessageWrapper.SetActive(true);
    }
}
