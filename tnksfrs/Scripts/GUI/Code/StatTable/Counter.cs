using System;
using UnityEngine;

public class Counter : MonoBehaviour
{
    public event Action OnStop;

    public bool IsActive
    {
        get; private set;
    }

    public int CountAtStart
    {
        get; private set;
    }

    public int CurrentCount
    {
        get; private set;
    }
	
	void Awake()
	{
	    IsActive = false;
	}

	public void StartTimer(int _count, bool showCounter)
	{
		if (IsInvoking())
			CancelInvoke();

        CountAtStart = _count;

		CurrentCount = _count + 1;

		this.InvokeRepeating(Count, 0, 1);

	    IsActive = true;
	}

	public void StopTimer()
	{
		CancelInvoke();
	    IsActive = false;
	}
	
	public void MoveToEnd()
	{
		CurrentCount = 0;
        Count();
        IsActive = false;
	}

	private void Count()
	{
		CurrentCount--;

	    if (CurrentCount > 0)
	        return;

	    CancelInvoke();

	    if (OnStop != null)
	        OnStop();

	    IsActive = false;
	}
}
