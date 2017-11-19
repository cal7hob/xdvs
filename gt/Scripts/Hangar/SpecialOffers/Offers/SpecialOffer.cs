using UnityEngine;

public abstract class SpecialOffer : MonoBehaviour
{
    protected int id;
    protected double endTime;
    protected SpecialOfferPrefab offerFrame;

    public int Id { get { return id; } }
    public double EndTime { get { return endTime; } }

    public long Remain
    {
        get
        {
            return (long)(EndTime - GameData.CorrectedCurrentTimeStamp);
        }
    }

    public virtual bool IsLimited { get { return false; } }

    protected virtual void Awake()
    {
        GetOfferFrame();
    }

    protected virtual void OnDestroy()
    {
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
        if (offerFrame != null)
        {
            offerFrame.SetActive(Remain > 0 && !IsLimited);
        }
    }

    protected virtual void ProfileChanged(EventId id, EventInfo info)
    {
        UpdateItem();
    }

    protected abstract void SetInfo(EventId eventId = 0, EventInfo info = null);
    protected abstract void SetPrice();
    protected abstract void SetSprite();
}
