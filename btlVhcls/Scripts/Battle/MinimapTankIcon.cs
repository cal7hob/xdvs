using UnityEngine;
using System.Collections;
using System;

public class MinimapTankIcon : MonoBehaviour 
{
    [SerializeField] tk2dSprite image;
    [SerializeField] GameObject radar;

    [Header("If colorize sprites of players on minimap")]
    [SerializeField] Color enemyColor;
    [SerializeField] Color friendColor;
    [SerializeField] Color meColor;

    [SerializeField] private int myIconOrder = 13;
    [SerializeField] private int otherPlayerIconOrder = 12;

    public GameObject chatMessageWrapper;//Common chat message indicator wrapper
    [SerializeField] tk2dUIToggleButtonGroup chatMessageCustomWrappers;//0 = me, 1 = friend, 2 = enemy

    private float pxCoordsX = 0f, pxCoordsY = 0f;
    private Vector3 targetlocalForward;
    private VehicleController player = null;
    private BattleChatPanelItemData chatMessageData;

    void Update () 
    {
        if (BattleController.MyVehicle == null || !player)
            return;

        pxCoordsX = -1 * Mathf.Abs(Minimap.realMapCornerTR.transform.position.x - player.transform.position.x) * Minimap.scaleKoefX;//Координаты умножаем на -1, потому что отсчет от правого верхнего угла
        pxCoordsY = -1 * Mathf.Abs(Minimap.realMapCornerTR.transform.position.z - player.transform.position.z) * Minimap.scaleKoefY;
        image.transform.localPosition = radar.transform.localPosition = new Vector3(pxCoordsX, pxCoordsY, image.transform.localPosition.z);
        
        if (player == BattleController.MyVehicle)//Rotate our vehicle icon
        {
            targetlocalForward = Minimap.realMapCornerBL.InverseTransformDirection(BattleController.MyVehicle.Body.forward);
            image.transform.localRotation = Quaternion.Euler (0, 0, Mathf.Rad2Deg * Mathf.Atan2 (targetlocalForward.z, targetlocalForward.x));

            targetlocalForward = Minimap.realMapCornerBL.InverseTransformDirection(BattleController.MyVehicle.Turret.forward);
            radar.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Rad2Deg * Mathf.Atan2(targetlocalForward.z, targetlocalForward.x) + 45);

        }

        #region Выключение итема чата
        if (chatMessageWrapper && chatMessageWrapper.activeSelf && chatMessageData != null && !chatMessageData.IsLive)
            chatMessageWrapper.SetActive(false);
        #endregion
    }

    public void Setup(int _playerId)
    {
        if (chatMessageWrapper)
            chatMessageWrapper.SetActive(false);

        player = BattleController.allVehicles[_playerId];
        radar.SetActive(player.IsMain);
        image.SortingOrder = player.IsMain ? myIconOrder : otherPlayerIconOrder;
        gameObject.name = player.data.playerName;

        if (GameData.IsGame(Game.SpaceJet | Game.BattleOfWarplanes | Game.WingsOfWar))
        {
            if (player.IsMain)
                image.color = meColor;
            else if (player.IsMainsFriend)
                image.color = friendColor;
            else
                image.color = enemyColor;
        }
        else
        {
            if (player.IsMain)
            {
                image.SetSprite("minimap_me");
                if(chatMessageCustomWrappers)
                    chatMessageCustomWrappers.SelectedIndex = 0;
                //DT.LogError("player {0} its me!", BattleController.GameStat[playerId].playerName);
            }
            else if (player.IsMainsFriend)
            {
                image.SetSprite("minimap_friend");
                if (chatMessageCustomWrappers)
                    chatMessageCustomWrappers.SelectedIndex = 1;
                //DT.LogError("player {0} its friend!", BattleController.GameStat[playerId].playerName);
            }
            else//Enemy
            {
                image.SetSprite("minimap_enemy");
                if (chatMessageCustomWrappers)
                    chatMessageCustomWrappers.SelectedIndex = 2;
                //DT.LogError("player {0} its enemy!", BattleController.GameStat[playerId].playerName);
            }
        }
    }

    public void SetupChatMessage(BattleChatPanelItemData data)
    {
        if (data == null || player == null )
            return;
        chatMessageData = data;
        chatMessageWrapper.SetActive(true);
    }
}
