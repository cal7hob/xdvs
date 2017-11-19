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

        // TODO: отрефакторить вместе с другими использованиями флага
        sprFlag.SafeSetSprite(
            string.IsNullOrEmpty(data.Country) || data.Country == "unknown"
                ? GameData.UNKNOWN_FLAG_NAME
                : data.Country, GameData.UNKNOWN_FLAG_NAME);

        lblPlayerNick.text = data.PlayerNickname;
        lblPlayerNick.color = GetColorByPlayerId(data.IsPhotonPlayerIdMain, data.IsPhotonPlayerIdMainsFriend);

        switch (data.messageId)
        {
            case BattleChatCommands.Id.Killing:
                var customData = data as BattleInfoPanelKillingItemData;

                if (customData == null)
                    return;

                sprIcon.gameObject.SetActive(true);
                lblMessage.color = GetColorByPlayerId(customData.IsVictimMain, customData.IsVictimMainsFriend);
                lblMessage.text = customData.VictimName;
                break;
            default:
                sprIcon.gameObject.SetActive(false);
                lblMessage.text = data.ChatMessage;
                lblMessage.color = normalChatMessageColor;
                break;
        }
    }

    private Color GetColorByPlayerId(bool vehicleIsMain, bool vehicleIsMainsFriend)
    {
        if (vehicleIsMain)
            return myNickColor;
        else if (vehicleIsMainsFriend)
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
    public int photonPlayerId; // Отправитель сообщения или убийца
    
    public BattleChatCommands.Id messageId;
    private float hideTime;

    public string PlayerNickname { get; private set; }

    public string Country { get; private set; }

    public bool IsPhotonPlayerIdMain { get; private set; }
    public bool IsPhotonPlayerIdMainsFriend { get; private set; }

    public string ChatMessage { get { return Localizer.GetText(string.Format("battleChatCommand_{0}", (int)messageId)); } }
    
    public bool IsLive { get { return Time.time < hideTime; } }

    public BattleChatPanelItemData(int _photonPlayerId, BattleChatCommands.Id _messageId, float showingTime)
    {
        VehicleController vehicle;
        if (!BattleController.allVehicles.TryGetValue(_photonPlayerId, out vehicle))
        {
            Debug.LogErrorFormat("No message owner in BattleController.allVehicles with id {0}", _photonPlayerId);
            return;
        }

        photonPlayerId = _photonPlayerId;
        messageId = _messageId;
        hideTime = showingTime + GameData.battleChatPanelItemLiveTime;

        PlayerNickname = vehicle.data.playerName;

        Country = !vehicle.data.hideMyFlag
            ? ((string) vehicle.data.country).ToLowerInvariant()
            : null;

        IsPhotonPlayerIdMain = vehicle.IsMain;
        IsPhotonPlayerIdMainsFriend = vehicle.IsMainsFriend;
    }
}

public class BattleInfoPanelKillingItemData: BattleChatPanelItemData
{
    public int victimId;

    public string VictimName { get; private set; }
    public bool IsVictimMain { get; private set; }
    public bool IsVictimMainsFriend { get; private set; }

    public BattleInfoPanelKillingItemData(int _killerId, BattleChatCommands.Id _messageId, float _showingTime, int _victimId) : 
        base(_killerId, _messageId, _showingTime)
    {
        victimId = _victimId;
        VictimName = BattleController.allVehicles[victimId].data.playerName;
        IsVictimMain = BattleController.allVehicles[victimId].IsMain;
        IsVictimMainsFriend = BattleController.allVehicles[victimId].IsMainsFriend;
    }
}