using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class AnimatedWindow : MonoBehaviour
{
    public Vector3 deltaPosition;
    public float timeToFinish;
    private Vector3 hidePosition;
    private Vector3 showPosition;
    public GameObject arrowIsOpened;
    public GameObject arrowIsClosed;
    public tk2dUIItem button;
    private const string REGKEY_SCORES_BOX_STATE = "ScoresBoxState";
    private Tweener tween;

    void Awake()
    {
        Dispatcher.Subscribe(EventId.ScoresBoxActivated, Handler);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.ScoresBoxActivated, Handler);
    }

    void OnDisable()
    {
        if (tween != null)
        {
            tween.Complete();
        }
    }

    void Handler(EventId _id, EventInfo _info = null)
    {
        SetStartPositionAndArrow();
        BoxStatus();
    }

    private void SetStartPositionAndArrow()
    {
        showPosition = transform.localPosition;
        transform.localPosition += deltaPosition;
        hidePosition = transform.localPosition;
        arrowIsOpened.SetActive(false);
        arrowIsClosed.SetActive(true);
    }

    private void BoxStatus()
    {
        if (HangarController.FirstEnter && ProfileInfo.Level <= GameData.hideScoresTillLevel &&
           !PlayerPrefs.HasKey(REGKEY_SCORES_BOX_STATE))
        {
            transform.localPosition = hidePosition;
            button.gameObject.SetActive(false);

        }
        else
        {
            button.gameObject.SetActive(true);
            if (PlayerPrefs.HasKey(REGKEY_SCORES_BOX_STATE) &&
             Convert.ToBoolean(PlayerPrefs.GetInt(REGKEY_SCORES_BOX_STATE)))
            {
                Btn_OnClick();
            }
        }
    }


    private void OnClick(tk2dUIItem _btn)
    {
        Btn_OnClick();
    }

    private void Btn_OnClick()
    {
        MenuController.CheckBoxSound();
        if (Convert.ToBoolean(PlayerPrefs.GetInt(REGKEY_SCORES_BOX_STATE)))
        {
            PlayerPrefs.SetInt(REGKEY_SCORES_BOX_STATE, Convert.ToByte(false));
            arrowIsOpened.SetActive(false);
            arrowIsClosed.SetActive(true);
            tween = transform.DOLocalMove(hidePosition, timeToFinish);
        }
        else
        {
            PlayerPrefs.SetInt(REGKEY_SCORES_BOX_STATE, Convert.ToByte(true));
            arrowIsClosed.SetActive(false);
            arrowIsOpened.SetActive(true);
            tween = transform.DOLocalMove(showPosition, timeToFinish);
        }



        //if (HelpTools.Vector3Approximately(transform.localPosition,hidePosition))
        //{
        //    PlayerPrefs.SetInt(REGKEY_SCORES_BOX_STATE, Convert.ToByte(true));
        //    arrowIsClosed.SetActive(false);
        //    arrowIsOpened.SetActive(true);
        //    tween = transform.DOLocalMove(showPosition, timeToFinish);
        //}
        //if (HelpTools.Vector3Approximately(transform.localPosition,showPosition))
        //{
        //    PlayerPrefs.SetInt(REGKEY_SCORES_BOX_STATE, Convert.ToByte(false));
        //    arrowIsOpened.SetActive(false);
        //    arrowIsClosed.SetActive(true);
        //    tween = transform.DOLocalMove(hidePosition, timeToFinish);
        //};

    }
}
