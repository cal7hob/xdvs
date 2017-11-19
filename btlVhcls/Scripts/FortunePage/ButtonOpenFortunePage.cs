using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonOpenFortunePage : MonoBehaviour
{
    public static ButtonOpenFortunePage Instance { get; private set; }

    [SerializeField] private tk2dTextMesh lblTime;
    [SerializeField] private TweenAlpha tweenAlphaScript;

    private void Awake()
    {
        Instance = this;
        lblTime.gameObject.SetActive(false);
        HangarController.OnTimerTick += OnTimerTick;
        Dispatcher.Subscribe(EventId.ProfileInfoLoadedFromServer, OnProfileInfoLoadedFromServer);
    }

    private void OnDestroy()
    {
        HangarController.OnTimerTick -= OnTimerTick;
        Dispatcher.Unsubscribe(EventId.ProfileInfoLoadedFromServer, OnProfileInfoLoadedFromServer);
        Instance = null;
    }

    private void OnEnable()
    {
        UpdateElements();
    }

    private void UpdateElements()
    {
        if (!gameObject.activeSelf || !HangarController.Instance || !HangarController.Instance.IsInitialized)
            return;
        lblTime.text = FortunePage.IsAllAttemptsUsed || !FortunePage.IsWaitingForNextAttempt ? "" : Clock.GetTimerString(FortunePage.TimeToNextAttempt);
        lblTime.gameObject.SetActive(lblTime.text.Length > 0);
        tweenAlphaScript.SetActiveAnimation(!FortunePage.IsAwardObtained || FortunePage.CanSpinTheRoulett);
    }

    private void OnTimerTick(double time)
    {
        UpdateElements();
    }

    private void OnProfileInfoLoadedFromServer(EventId id, EventInfo info)
    {
        UpdateElements();
    }

    private void OnClick(tk2dUIItem btn)
    {
        GUIPager.SetActivePage("FortunePage");
    }
}
