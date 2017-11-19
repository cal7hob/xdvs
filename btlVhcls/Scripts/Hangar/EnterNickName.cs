using UnityEngine;
using System.Collections.Generic;

public class EnterNickName : HangarPage {

    [SerializeField] private tk2dTextMesh playerName;
    [SerializeField] private tk2dTextMesh lblUserName;
    [SerializeField] private tk2dUITextInput thisUiItem;
    [SerializeField] private tk2dTextMesh nickChangePrice;
    [SerializeField] private tk2dSprite nickChangeCurrencySprite;

    public static EnterNickName Instance { get; private set; }

    protected override void Create ()
    {
        base.Create ();
        Instance = this;
    }

    protected override void Init ()
    {
        base.Init ();
        thisUiItem.OnTextChange += OnNickChanged;
    }

    protected override void Destroy ()
    {
        Instance = null;
        base.Destroy ();
    }

    protected override void ProfileChanged () {
        base.ProfileChanged ();

        if (GUIPager.ActivePageName == "EnterName" && ProfileInfo.nickEntered) {
            GUIPager.SetActivePage ("MainMenu");
        }
    }

    public void PopUpNicknameWindow(EventId id = 0, EventInfo info = null)
    {
        if (ProfileInfo.nickRejected || ProfileInfo.nickEntered
            || ProfileInfo.TutorialIndex != (int)Tutorials.enterName)
            return;

        nickChangePrice.text = GameData.changeNickPrice.LocalizedValue;
        nickChangeCurrencySprite.SetSprite(nickChangeCurrencySprite.Collection,
            GameData.changeNickPrice.currency == ProfileInfo.PriceCurrency.Gold ? "gold" : "silver");

        GUIPager.SetActivePage("EnterName");
    }

    private void OnNickChanged(tk2dUITextInput textInput)
    {
        if (thisUiItem.Text.Length > Settings.MAX_NAME_LENGTH)
            thisUiItem.Text = thisUiItem.Text.Substring(0, Settings.MAX_NAME_LENGTH);
    }

    #region Обработка кнопок окна
    private void Submit()
    {
        Http.Manager.ReportStats ("enterNicknamePage", "submitNewNickname", new Dictionary<string, string> () {
            { "nickname", playerName.text }
        });
        Settings.Instance.SetNickName(playerName.text , () =>
        {
            GUIPager.SetActivePage("MainMenu");
            Dispatcher.Send(EventId.NickNameManuallyChanged, new EventInfo_SimpleEvent());
        });
    }

    private void EnterNickLater()
    {
        Http.Manager.RejectNickName((fResult, response) =>
        {
            if (fResult)
            {
                GUIPager.SetActivePage("MainMenu");
                Dispatcher.Send(EventId.NickNameManuallyChanged, new EventInfo_SimpleEvent());
            }
            else
            {
                Http.Manager.ReportException("EnterNickName.EnterNickLater", new System.Exception("EnterNickName.EnterNickLater"));
            }

        });
    }

    #endregion
}
