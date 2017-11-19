using System;
using UnityEngine;
using System.Collections;
using DG.Tweening;

public class SmoothTransformPosition : MonoBehaviour
{   // Скрипт для плавного выплывания окошек на сцену
    // deltaStartpos задает место откуда будет выплывать окошко. Допустим deltaStartpos = Vector3(0,-800,0) - окошко будет выплывать с расстояния в 800 единиц снизу. 
    public Vector3 deltaStartpos; /// <summary>
                                  /// Тут много всего, что осталось от анимации Lerpом. Теперь всё на DOTWeen и можно выпиливать почти все поля и половину скрипта. Но делать этого я пока не буду, 
                                  /// поскольку, если где то возникнет какая то проблема с ним - чтобы поля заново не выставлять в каждом анимируемом окошке. 20.04.2017. Если спустя месяц всё ништячек - выпиливаем отсюда всё. 
                                  /// </summary>
    public float delayBefore = 0f;
    public bool activateOnEnable = true;
    public bool deactivateOnPPkey = false;
    public bool ActivateOnEvent;
    public const float tweenDuration = 0.7f;
    public Tweener tweener;
    public EventId[] id;
    private Vector3 endPosition;
    private bool started = false;
    private Coroutine moveRoutine = null;

    private void StartMoving()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }
        moveRoutine = StartCoroutine(MoveRoutine());
    }
    


    private IEnumerator MoveRoutine()
    {
        float fracJourney = 0;
        Vector3 startPosition = endPosition + deltaStartpos;
        transform.localPosition = startPosition;
        yield return new WaitForSeconds(delayBefore);
        tweener = transform.DOLocalMove(endPosition, tweenDuration);
        moveRoutine = null;
        yield break;
    }

    private void Awake()
    {
        if (ActivateOnEvent)
        {
            foreach (var _id in id)
            {
                Dispatcher.Subscribe(_id, EventActivator);
            }
        }
    }

    private void Start()
    {
        SetEndPosition();
        started = true;
        if (!activateOnEnable)
        {
            return;
        }
        StartMoving();
    }

    private void SetEndPosition()
    {
        endPosition = transform.localPosition;
    }

    private void OnEnable()
    {
        if (!started)
        {
            return;
        }
        if (!activateOnEnable)
        {
            return;
        }
        if (deactivateOnPPkey)
        //Метод для ScoreBox. Пока только там требуется такое исключение. Если понадобится где то еще - расширим. 
        {
            if ((PlayerPrefs.HasKey("ScoresBoxState")) && (Convert.ToBoolean(PlayerPrefs.GetInt("ScoresBoxState"))))
            {
                return;
            }
            else
            {
                SetEndPosition();
            }
        }
        DOTween.Clear();
        StartMoving();
    }

    public void OnDisable()
    {
        if (moveRoutine != null)
        {
            DOTween.Clear();
            StopCoroutine(moveRoutine);
            moveRoutine = null;
            transform.localPosition = endPosition + deltaStartpos;
        }
    }
    private void EventActivator(EventId _id, EventInfo _info)
    {
        if (!started)
        {
            return;
        }
        StartMoving();
    }

    private void OnDestroy()
    {
        // Dispatcher.Unsubscribe(EventId.ResolutionChanged, ResChanged);
        if (ActivateOnEvent)
        {
            foreach (var _id in id)
            {
                Dispatcher.Unsubscribe(_id, EventActivator);
            }
        }
    }
}
