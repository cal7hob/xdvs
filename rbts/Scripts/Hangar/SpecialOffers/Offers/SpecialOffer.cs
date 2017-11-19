using System;
using UnityEngine;

public abstract class SpecialOffer : MonoBehaviour
{
    protected int id;
    protected double endTime;
    protected SpecialOfferPrefab offerFrame;

    public int Id { get { return id; } }
    public double EndTime { get { return endTime; } }

    public virtual long Remain
    {
        get
        {
            return (long)(EndTime - GameData.CorrectedCurrentTimeStamp);
        }
    }

    public virtual bool IsLimited { get; protected set; }

    protected virtual void Awake()
    {
        IsLimited = false;
    }

    protected virtual void Start()
    {
        GetOfferFrame();
        Messenger.Subscribe(EventId.ProfileInfoLoadedFromServer, ProfileChanged);
    }

    protected virtual void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.ProfileInfoLoadedFromServer, ProfileChanged);
        UnsubscribeFromTimer();
    }

    public virtual void SubscribeOnTimer()
    {
        HangarController.OnTimerTick += OnTick;
    }

    public void UnsubscribeFromTimer()
    {
        HangarController.OnTimerTick -= OnTick;
    }

    private void GetOfferFrame()
    {
        offerFrame = GetComponent<SpecialOfferPrefab>();
    }

    protected virtual void InitializeOfferFrame()
    {
        GetOfferFrame();
        SetBtn();
        SetPrice();
        SetSprite();
        SetInfo();

        SetOfferFrameName();
    }

    protected virtual void OnTick(double tick)
    {
        offerFrame.timer.text = Clock.GetTimerString(Remain);
        
        if (Remain < 0)
        {
            UnsubscribeFromTimer();
            UpdateItem();
        }
    }

    protected virtual void SetOfferFrameName()
    {
        name = GetType().Name;
    }

    protected virtual void SetBtn()
    {
        offerFrame.lblBuy.text = Localizer.GetText("lblBuy");
    }

    public virtual void UpdateItem()
    {
        if ((Remain <= 0) || IsLimited)
        {
            offerFrame.Hide();
        }
        else {
            offerFrame.Show();
        }
    }

    protected abstract void SetInfo(EventId eventId = 0, EventInfo info = null);
    protected abstract void SetPrice();
    protected abstract void SetSprite();
    
    protected virtual void ProfileChanged (EventId id, EventInfo info)
    {
        UpdateItem();
    }
}
