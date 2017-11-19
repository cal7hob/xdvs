using UnityEngine;
using System.Collections;

public class BattleChatPanelItem : MonoBehaviour
{
    [SerializeField]
    private GameObject wrapper;
    [SerializeField]
    private tk2dBaseSprite sprFlag;
    [SerializeField]
    private tk2dTextMesh lblPlayerNick;
    [SerializeField]
    private tk2dTextMesh lblMessage;
    [SerializeField]
    private float lblPlayerNickMaxWidth = 300;
    [SerializeField]
    private Color32 ownColor;
    [SerializeField]
    private Color32 allyColor;
    [SerializeField]
    private Color32 enemyColor;
    [SerializeField]
    private Color32 messageColor;
    [SerializeField]
    private GameObject skull;
    [SerializeField]
    private GameObject chatIcon;
    [SerializeField]
    private UniAligner aligner;


    private Vector3 mineSize = new Vector3(1.1f, 1.1f, 1);
    private Vector3 skullSize = new Vector3(1.2f, 1.2f, 1);

    private BattleChatPanelItemData data = null;


    //public BattleChatCommands.Id id;

    public void Setup(BattleChatPanelItemData _data)
    {
        data = _data;
        sprFlag.SetSprite(data.Country);
        if(!BattleController.allVehicles.ContainsKey(data.photonPlayerId) && BattleController.GameStat.ContainsKey((int)data.messageId))
        {
            return;
        }
        if (BattleController.allVehicles.ContainsKey(data.photonPlayerId) &&
            BattleController.allVehicles[data.photonPlayerId].data.hideMyFlag)
        {
            sprFlag.gameObject.SetActive(false);
        }
        else
        {
            sprFlag.gameObject.SetActive(true);
        }
        lblPlayerNick.text = data.PlayerNickname;
        HelpTools.ClampLabelText(lblPlayerNick, lblPlayerNickMaxWidth);


        if (BattleController.MyVehicle == BattleController.allVehicles[data.photonPlayerId])
        {
            lblPlayerNick.color = ownColor;
        }
        else if (BattleController.Instance.IsTeamMode &&
                 BattleController.allVehicles[data.photonPlayerId].data.teamId == BattleController.MyVehicle.data.teamId)
        {
            lblPlayerNick.color = allyColor;
        }
        else
        {
            lblPlayerNick.color = enemyColor;
        }

        if (data.IsNecrolog)
        {
            chatIcon.SetActive(false);
            skull.SetActive(true);
            lblMessage.text = BattleController.GameStat[(int)data.messageId].playerName;
            var spr = skull.GetComponent<tk2dSprite>();
            if (_data.MineKilledMe)
            {
                spr.SetSprite("bonus_mine");
                spr.scale = mineSize;
            }
            else
            {
                spr.SetSprite("killerIcon");
                spr.scale = skullSize;
            }
            aligner.Align();

            if (BattleController.MyVehicle == BattleController.allVehicles[(int)data.messageId])
            {
                lblMessage.color = ownColor;
            }
            else if (BattleController.Instance.IsTeamMode &&
                     BattleController.allVehicles[(int)data.messageId].data.teamId == BattleController.MyVehicle.data.teamId)
            {
                lblMessage.color = allyColor;
            }
            else
            {
                lblMessage.color = enemyColor;
            }

        }
        else
        {

            lblMessage.text = data.Message;
            lblMessage.color = messageColor;
            skull.SetActive(false);
            chatIcon.SetActive(true);
        }



    }

    public void SetActive(bool en)
    {
        wrapper.SetActive(en);
    }

    private void Update()
    {
        if (!wrapper.activeSelf || data == null)
            return;

        if (!data.IsLive)
            wrapper.SetActive(false);
    }
}

public class BattleChatPanelItemData
{
    public int photonPlayerId;
    public BattleChatCommands.Id messageId;
    public float showingTime = 0;
    public bool IsNecrolog;
    public bool MineKilledMe;

    public string Country { get { return ((string)BattleController.allVehicles[photonPlayerId].data.country).ToLower(); } }
    public string Message { get { return Localizer.GetText(string.Format("battleChatCommand_{0}", (int)messageId)); } }
    public string PlayerNickname { get { return BattleController.allVehicles[photonPlayerId].data.playerName; } }
    public bool IsLive { get { return Time.realtimeSinceStartup < showingTime + GameData.battleChatPanelItemLiveTime; } }

    public BattleChatPanelItemData(int _photonPlayerId, BattleChatCommands.Id _messageId, float _showingTime, bool _IsNecrolog, bool _MineKilledMe)
    {
        photonPlayerId = _photonPlayerId;
        messageId = _messageId;
        showingTime = _showingTime;
        IsNecrolog = _IsNecrolog;
        MineKilledMe = _MineKilledMe;

    }
}
