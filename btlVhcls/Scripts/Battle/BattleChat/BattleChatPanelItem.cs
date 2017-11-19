using UnityEngine;
using System.Collections;

public class BattleChatPanelItem : MonoBehaviour
{
    [SerializeField] private GameObject wrapper;
    [SerializeField] private tk2dBaseSprite sprFlag;
    [SerializeField] private tk2dBaseSprite sprIcon;
    [SerializeField] private tk2dTextMesh lblPlayerNick;
    [SerializeField] private tk2dTextMesh lblMessage;
    [SerializeField] private Color myNickColor = Color.white;
    [SerializeField] private Color friendNickColor = Color.white;
    [SerializeField] private Color enemyNickColor = Color.white;
    [SerializeField] private Color normalChatMessageColor = Color.white;

    private BattleChatPanelItemData data = null;

    public void Setup(BattleChatPanelItemData _data)
    {
        data = _data;

        sprFlag.SetSprite(data.Country);
        lblPlayerNick.text = data.PlayerNickname;
        lblPlayerNick.color = GetColorByPlayerId(_data.photonPlayerId);

        if (data.messageId == BattleChatCommands.Id.Killing)//killing message
        {
            BattleInfoPanelKillingItemData customData = (BattleInfoPanelKillingItemData)data;
            BattleChatPanel.ShellData shellData = BattleChatPanel.Instance.GetShellData(customData.shellType);
            sprIcon.SetSprite(shellData != null ? shellData.spriteName : "bonus_kill");
            sprIcon.gameObject.SetActive(true);
            lblMessage.color = GetColorByPlayerId(customData.victimId);
            lblMessage.text = customData.VictimName;
        }
        else//chat message
        {
            sprIcon.gameObject.SetActive(false);
            lblMessage.text = data.ChatMessage;
            lblMessage.color = normalChatMessageColor;
        }
    }

    private Color GetColorByPlayerId(int playerId)
    {
        if (!BattleController.allVehicles.ContainsKey(playerId) || !BattleController.allVehicles[playerId])//fix exception
            return enemyNickColor;
        else if (BattleController.allVehicles[playerId].IsMain)
            return myNickColor;
        else if (BattleController.allVehicles[playerId].IsMainsFriend)
            return friendNickColor;
        else
            return enemyNickColor;
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
    public int photonPlayerId;//chat message sender or killer
    
    public BattleChatCommands.Id messageId;
    public float showingTime = 0;
    

    public string Country
    {
        get
        {
            if (!BattleController.allVehicles.ContainsKey(photonPlayerId))
            {
                Debug.LogErrorFormat("There isn't player {0} in BattleController.allVehicles!", photonPlayerId);
                return GameData.UNKNOWN_FLAG_NAME;
            }
                
            return BattleController.allVehicles[photonPlayerId].data.hideMyFlag ? 
                GameData.UNKNOWN_FLAG_NAME :
             ((string)BattleController.allVehicles[photonPlayerId].data.country).ToLower();
        }
    }
    public string ChatMessage { get { return Localizer.GetText(string.Format("battleChatCommand_{0}", (int)messageId)); } }
    public string PlayerNickname { get { return BattleController.allVehicles.ContainsKey(photonPlayerId) ? BattleController.allVehicles[photonPlayerId].data.playerName.ToString() : ""; } }
    public bool IsLive { get { return Time.realtimeSinceStartup < showingTime + GameData.battleChatPanelItemLiveTime; } }

    public BattleChatPanelItemData(int _photonPlayerId, BattleChatCommands.Id _messageId, float _showingTime)
    {
        photonPlayerId = _photonPlayerId;
        messageId = _messageId;
        showingTime = _showingTime;
    }
}

public class BattleInfoPanelKillingItemData: BattleChatPanelItemData
{
    public int victimId;
    public GunShellInfo.ShellType shellType;

    public string VictimName { get { return BattleController.allVehicles.ContainsKey(victimId) ? BattleController.allVehicles[victimId].data.playerName.ToString() : ""; } }

    public BattleInfoPanelKillingItemData(int _killerId, BattleChatCommands.Id _messageId, float _showingTime, int _victimId, GunShellInfo.ShellType _shellType ) : 
        base(_killerId, _messageId, _showingTime)
    {
        victimId = _victimId;
        shellType = _shellType;
    }
}