using UnityEngine;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;

public class GameAccount : MonoBehaviour
{
    public Color header_gameNameColor = Color.white;
    public Color header_textColor = Color.white;
    [SerializeField] private ActivatedUpDownButton btnLogin;
    [SerializeField] private ActivatedUpDownButton btnRegister;
    [SerializeField] private ActivatedUpDownButton btnPassRecovery;
    [SerializeField] private tk2dUITextInput email;
    [SerializeField] private tk2dUITextInput password;
    [SerializeField] private tk2dTextMesh lblGameAccountTitle;
    [SerializeField] private TweenBase emailTween;
    [SerializeField] private TweenBase passTween;

    public static string emailRegexString = @".@.";
    //public static string loginRegexString = @"^[a-z0-9]*$";
    public static string passRegexString = @".";

    //public static Regex loginRegex = new Regex(loginRegexString, RegexOptions.IgnoreCase);
    public static Regex passRegex = new Regex(passRegexString, RegexOptions.IgnoreCase);
    public static Regex emailRegex = new Regex(emailRegexString, RegexOptions.IgnoreCase);
    //public static Regex allCharsRegex = new Regex(@"^(\w)+$");


    void Awake()
    {
        //Instance = this;
        SetCaption();
        Messenger.Subscribe(EventId.PageChanged, OnWindowOpen);
        Messenger.Subscribe(EventId.OnLanguageChange, OnLanguageChange);
        btnLogin.Activated = btnRegister.Activated = btnPassRecovery.Activated = true;
    }

    void OnDestroy()
    {
        //Instance = null;
        Messenger.Unsubscribe(EventId.PageChanged, OnWindowOpen);
        Messenger.Unsubscribe(EventId.OnLanguageChange, OnLanguageChange);
    }

    private void OnTextChange()
    {
        //btnLogin.Activated = IsEmailValid() && IsPassValid();
        //btnRegister.Activated = IsEmailValid() && string.IsNullOrEmpty(password.Text);
        //btnPassRecovery.Activated = IsEmailValid();
    }

    private bool IsEmailValid()
    {
        return emailRegex.IsMatch(email.Text);
    }

    private bool IsPassValid()
    {
        return password.Text.Length >= 4 &&
            password.Text.Length <= 20 &&
            passRegex.IsMatch(password.Text);
    }

    private void OnLoginClick(tk2dUIItem btn)
    {
        if (string.IsNullOrEmpty(email.Text) || string.IsNullOrEmpty(password.Text))
        {
            if (emailTween && string.IsNullOrEmpty(email.Text))
                emailTween.Play();
            if (passTween && string.IsNullOrEmpty(password.Text))
                passTween.Play();
            return;
        }
            
        SocialSettings.GetSocialService().Logout();

        Http.Manager.Login(email.Text, password.Text, (fResult, response) =>
        {
            if (fResult)
            {
                if (!ProfileInfo.LoadProfile (response.Data["profile"]))
                {
                    Debug.LogError ("Can't load profile! result: " + response.text);

                    Http.Manager.ReportStats ("Login", "loadFailed",
                        new Dictionary<string, string>
                        {
                            { "response", response.text }
                        });
                    return;
                }
                GUIPager.SetActivePage("MainMenu");
            }
            else
                MessageBox.Show(MessageBox.Type.Info, Http.Manager.LocalizeServerErrCode(response.ServerError));
        });

    }

    private void OnRegisterClick(tk2dUIItem btn)
    {
        if (string.IsNullOrEmpty(email.Text) )
        {
            if (emailTween && string.IsNullOrEmpty(email.Text))
                emailTween.Play();
            return;
        }

        Http.Manager.Register(email.Text, (fResult, response) =>
        {
            if (fResult)
            {
                MessageBox.Show(MessageBox.Type.Info, Localizer.GetText("successfulRegistrationMessage"),
                    (MessageBox.Answer answer) =>
                    {
                        GUIPager.SetActivePage("MainMenu");
                    });
            }
            else
                MessageBox.Show(MessageBox.Type.Info, Http.Manager.LocalizeServerErrCode(response.ServerError));
        });
    }

    private void OnPassRecoveryClick(tk2dUIItem btn)
    {
        Http.Manager.OpenURL(Http.Manager.ROUTE_RESETPASSWORD);
    }

    private void OnWindowOpen(EventId id, EventInfo info)
    {
        if (GUIPager.ActivePage != "GameAccount")
            return;
        UpdateFields();
    }

    private void UpdateFields()
    {
        if(!string.IsNullOrEmpty(ProfileInfo.PlayerEmail))
            email.Text = ProfileInfo.PlayerEmail;
        OnTextChange();
    }

    private void SetCaption()
    {
        if (Localizer.Loaded)
            lblGameAccountTitle.text = Localizer.GetText("lblGameAccountTitle", header_gameNameColor.To2DToolKitColorFormatString(), Application.productName, header_textColor.To2DToolKitColorFormatString());
    }

    private void OnLanguageChange(EventId evId, EventInfo ev)
    {
        SetCaption();
    }
}
