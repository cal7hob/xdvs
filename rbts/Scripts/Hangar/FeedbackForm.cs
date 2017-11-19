using UnityEngine;

public class FeedbackForm : MonoBehaviour
{
    public tk2dUITextInput emailTextInput;
    public tk2dUITextInput messageTextInput;

    private const int MAX_EMAIL_LENGTH = 320;
    private const int MAX_MESSAGE_LENGTH = 300;

    void Awake() { Instance = this; }

    void Start()
    {
        messageTextInput.OnTextChange += input =>
        {
            if (input.Text.Length > MAX_MESSAGE_LENGTH)
                input.Text = input.Text.Substring(0, MAX_MESSAGE_LENGTH);
        };

        //#if UNITY_WEBPLAYER

        //emailTextInput.enabled = true;

        emailTextInput.OnTextChange += input =>
        {
            if (input.Text.Length > MAX_EMAIL_LENGTH)
                input.Text = input.Text.Substring(0, MAX_EMAIL_LENGTH);
        };

        //#endif
    }

    public static FeedbackForm Instance { get; private set; }

    public void Show()
    {
        GUIPager.SetActivePage("FeedbackForm", true, GameData.IsGame(Game.IronTanks));
    }

    public void Close(tk2dUIItem tk2dUiItem) { GUIPager.Back(); }

    public void Report(tk2dUIItem tk2dUiItem)
    {
        if (messageTextInput.Text.Length == 0)
        {
            MessageBox.Show(MessageBox.Type.Info, Localizer.GetText("FeedbackMessageValidationError"));
            return;
        }

        Http.Request request = Http.Manager.Instance().CreateRequest("/player/feedback");
        request.Form.AddField("message", messageTextInput.Text);

        //#if UNITY_WEBPLAYER

        if (!emailTextInput.Text.Contains("@"))
        {
            MessageBox.Show(MessageBox.Type.Info, Localizer.GetText("EmailValidationError"));
            return;
        }

        request.Form.AddField("email", emailTextInput.Text);

        //#endif

        Http.Manager.StartAsyncRequest(
            request,
            successCallback: result => Debug.Log("Feedback successfully submitted to the server."),
            failCallback: result => Debug.LogWarning("Feedback's submission request failed with error: " + result.error));
    }
}
