using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Http;

public class DailyBonusPage : HangarPage, IQueueablePage
{
    [SerializeField] private Factory factory;
    [SerializeField] private tk2dTextMesh header;
    [SerializeField] private Color header_firstStringColor = Color.white;
    [SerializeField] private Color header_playEverydayColor = Color.white;
    [SerializeField] private Color header_gameNameColor = Color.white;
    [SerializeField] private Color header_toGetBonusColor = Color.white;

    private bool btnDisabled = false;//Чтобы не обрабатывать клики пока ждем ответа от сервера

    protected override void Show()
    {
        base.Show();
        if (header != null)
            header.text = Localizer.GetText("lblBonusText",
                header_firstStringColor.To2DToolKitColorFormatString(),
                header_playEverydayColor.To2DToolKitColorFormatString(),
                header_gameNameColor.To2DToolKitColorFormatString(),
                Application.productName,
                header_toGetBonusColor.To2DToolKitColorFormatString());

        #region Инстанирование объектов
        //ProfileInfo.dailyBonusIndex = 30;//to test 
        //ProfileInfo.dailyBonusMissed = 1;//to test 
        if (factory.Items.Count == 0 && GameData.dailyBonusInfos != null)
            factory.CreateAll(GameData.dailyBonusInfos);
        
        factory.ScroolToItem(ProfileInfo.dailyBonusDay - 1);
        #endregion
    }

    private void OnTakeBonusClick(tk2dUIItem btn)
    {
        if (btnDisabled)
            return;

        try
        {
            btnDisabled = true;
            var request = Http.Manager.Instance().CreateRequest("/player/getDailyBattleBonus");
            request.Form.AddField("playerId", ProfileInfo.profileId);
            Http.Manager.StartAsyncRequest(request,
                successCallback: (Response result) =>
                {
                    btnDisabled = false;
                    GUIPager.ToMainMenu();
                    Dispatcher.Send(EventId.DailyBonusTaken, new EventInfo_I(ProfileInfo.dailyBonusDay));
                },
                failCallback: (Response result) =>
                {
                    btnDisabled = false;
                    Debug.LogError("Failed to obtain daily bonus");
                });
        }
        catch (System.Exception e)
        {
            Http.Manager.ReportException("DailyBonus.TakeDailyBonusBtn", e);
            btnDisabled = false;
        }
    }

    public void BeforeActivation() { }

    public void Activated() { }
}
