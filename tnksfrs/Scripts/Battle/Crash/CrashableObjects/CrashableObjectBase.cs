using UnityEngine;
using System.Collections.Generic;
using XD;

public abstract class CrashableObjectBase : MonoBehaviour, ICrashableObject
{
    [Header("New system (reduce memory)")]
    [SerializeField]
    [SoundMixerSelector(SoundMixer.Crash, SoundMixer.Explosion)]
    private int[]               clips = null;

    [Header("Old system (increase memory)")]
    [SerializeField]
    private SoundMixer          mixer = SoundMixer.Crash;
    [SerializeField]
    private AudioClip[]         sounds = null;

    private bool                isCrashed = false;
    
    #region ISender
    public string Description
    {
        get
        {
            return "[CrashableObjectBase] " + name;
        }

        set
        {
            name = value;
        }
    }

    private List<ISubscriber> subscribers = null;

    public List<ISubscriber> Subscribers
    {
        get
        {
            if (subscribers == null)
            {
                subscribers = new List<ISubscriber>();
            }
            return subscribers;
        }
    }

    public void AddSubscriber(ISubscriber subscriber)
    {
        if (Subscribers.Contains(subscriber))
        {
            return;
        }
        Subscribers.Add(subscriber);
    }

    public void RemoveSubscriber(ISubscriber subscriber)
    {
        Subscribers.Remove(subscriber);
    }

    public void Event(Message message, params object[] parameters)
    {
        for (int i = 0; i < Subscribers.Count; i++)
        {
            Subscribers[i].Reaction(message, parameters);
        }
    }
    #endregion

    protected const float SOUND_VOLUME_RATIO = 1.0f;    

    protected virtual void CrashItself(Collider collider)
    {
        Event(Message.ObjectCrashed);
    }

    protected abstract void RestoreItself();

    public void Crash(Collider collider)
    {
        if (isCrashed)
        {
            return;
        }

        isCrashed = true;

        CrashItself(collider);
    }

    public void Restore()
    {
        isCrashed = false;
        RestoreItself();
    }

    protected void PlaySounds()
    {
        if (clips != null && clips.Length > 0)
        {
            StaticType.Sounds.Instance<ISounds>().Play(clips.GetRandom(), transform.position);
        }
        else
        {
            if (sounds.Length > 0)
            {
                StaticType.Sounds.Instance<ISounds>().Play(sounds.GetRandom(), mixer, transform.position);                
            }
        }
    }

	protected virtual void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireCube (transform.position, new Vector3 (2, 5, 2));
	}
}
