using Tanks.Models;
using UnityEngine;

public class ChatItemRequestToJoinClan : ChatItem
{
    [SerializeField] private Request request;

    [SerializeField] private tk2dUIItem applicantUiItem;
    [SerializeField] private tk2dUIItem btnOkUiItem;
    [SerializeField] private tk2dUIItem btnCancelUiItem;

    protected override void Awake()
    {
        base.Awake();
        applicantUiItem.OnClickUIItem += OnApplicantClickUIItemHandler;
        btnOkUiItem.OnClickUIItem += OnApproveClanRequest;
        btnCancelUiItem.OnClickUIItem += OnRejectClanRequest;
    }

    private void OnDestroy()
    {
        parentChatPage.containerSizer.RemoveLayout(layout);
        parentChatPage.containerSizer.Refresh();
        parentChatPage.requests.Remove(request);

        applicantUiItem.OnClickUIItem -= OnApplicantClickUIItemHandler;
        btnOkUiItem.OnClickUIItem -= OnApproveClanRequest;
        btnCancelUiItem.OnClickUIItem -= OnRejectClanRequest;
    }

    public void Init(Request request)
    {
        base.Init(request.Applicant);
        this.request = request;
    }

    private void OnApplicantClickUIItemHandler(tk2dUIItem clickedUiItem)
    {
        ChatMenuBehaviour.Instance.ShowContextMenu(clickedUiItem, layout, player);
    }

    private void OnApproveClanRequest(tk2dUIItem uiItem)
    {
        //Debug.LogWarning("OnApproveClanRequest: " + request.Id);

        //MessageBox.Show(MessageBox.Type.Info,
        //    Localizer.GetText("ChatApprovedRequestToJoinClan", request.Applicant.NickName));
        //Destroy();

        var req = Http.Manager.Instance().CreateRequest("/player/approveClanRequest");

        req.Form.AddField("requestId", request.Id);

        StartCoroutine(req.Call(
            successCallback: delegate (Http.Response result)
            {
                MessageBox.Show(MessageBox.Type.Info,
                    Localizer.GetText("ChatApprovedRequestToJoinClan", request.Applicant.NickName));
                Destroy(gameObject);
            },
            failCallback: delegate (Http.Response result)
            {
                Debug.LogError("Can't approve request to join clan. Error: " + result.error);
            }));
    }

    private void OnRejectClanRequest(tk2dUIItem uiItem)
    {
        //Debug.LogWarning("OnRejectClanRequest: " + request.Id);

        //MessageBox.Show(MessageBox.Type.Info,
        //    Localizer.GetText("ChatRejectedRequestToJoinClan", request.Applicant.NickName));
        //Destroy();

        var req = Http.Manager.Instance().CreateRequest("/player/rejectClanRequest");

        req.Form.AddField("requestId", request.Id);

        StartCoroutine(req.Call(
            successCallback: delegate (Http.Response result)
            {
                MessageBox.Show(MessageBox.Type.Info,
                    Localizer.GetText("ChatRejectedRequestToJoinClan", request.Applicant.NickName));
                Destroy(gameObject);
            },
            failCallback: delegate (Http.Response result)
            {
                Debug.LogError("Can't reject request to join clan. Error: " + result.error);
            }));
    }
}