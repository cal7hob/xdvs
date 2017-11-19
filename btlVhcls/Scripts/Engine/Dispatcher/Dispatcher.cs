using System;
using System.IO;
using ExitGames.Client.Photon;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Disconnect;

using UnityEngine.SceneManagement;


public partial class Dispatcher : MonoBehaviour
{
	private const int PRIORITY_COUNT = 5;
	private const int NORMAL_PRIORITY = 3;

	public enum EventTargetType
	{
		ToNoone = 0,
		Local = 1,
		ToAll = 2,
		ToOthers = 3,
		ToSpecific = 4,
		ToMaster = 5,
	}

	public class PhotonEventAdapter
	{
		public int eventId;
		public EventInfo eventInfo;
		

		public PhotonEventAdapter(int id, EventInfo info)
		{
			eventId = id;
			eventInfo = info;
		}

		public static byte[] SerializeAdapter(object customObject)
		{
			PhotonEventAdapter adapter = customObject as PhotonEventAdapter;
			byte[] serialized = adapter.eventInfo.Serialize();
			MemoryStream ms = new MemoryStream(4 + (serialized != null ? serialized.Length : 0));
			ms.Write(BitConverter.GetBytes(adapter.eventId), 0, 4);
			if (serialized != null)
				ms.Write(serialized, 0, serialized.Length);
			
			return ms.ToArray();
		}

		public static PhotonEventAdapter DeserializeAdapter(byte[] bytes)
		{
			int eventId = BitConverter.ToInt32(bytes, 0);
			EventInfo eventInfo = EventInfo.CommonDeserialize(eventId, bytes, 4);
			return eventInfo == null ? null : new PhotonEventAdapter(eventId, eventInfo);
		}
	}

    public delegate void EventSubscribeHandler(EventId id, EventInfo info);

    // Dispatcher itself
    public List<EventId> eventsToLog;
    public string checkSceneName;
    public bool showEventSent;
    public bool showSubscriptions;
    private const byte DISPATCHER_EVENT_CODE = 1;
	private static Dictionary<int, List<EventSubscribeHandler>[]> handlers;
	private static Dispatcher instance;
	
	/* UNITY SECTION */
	void Awake()
	{
        #if UNITY_EDITOR
	    if (showSubscriptions)
	    {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
	    }
        #endif
        if (instance != null)
	    {
	        Destroy(this);
            return;
	    }

	    instance = this;

        GameData.Init ();
        if (PhotonNetwork.OnEventCall == null)
			PhotonNetwork.OnEventCall += OnPhotonEvent;
		
		handlers = new Dictionary<int, List<EventSubscribeHandler>[]>(32);
		if (!Debug.isDebugBuild)
			showEventSent = false;
	}

	/* PUBLIC SECTION */
	public static void Subscribe(EventId id, EventSubscribeHandler handler, int order = NORMAL_PRIORITY)
	{
	    if (order < 0 || order > PRIORITY_COUNT - 1)
		{
			DT.LogError("Dispatcher: incorrect subscribe priority ({0}!", order);
			return;
		}

        int intId = (int)id;
        List<EventSubscribeHandler>[] foundHandler;
        if (!handlers.TryGetValue(intId, out foundHandler))
	    {
	        foundHandler = new List<EventSubscribeHandler>[PRIORITY_COUNT];
	        for(int i = 0; i < foundHandler.Length; i++)
	        {
                foundHandler[i] = new List<EventSubscribeHandler>();
	        }

            handlers.Add(intId, foundHandler);
	    }

	    foundHandler[order].Add(handler);
	}

	public static void Unsubscribe(EventId id, EventSubscribeHandler handler)
	{
        List<EventSubscribeHandler>[] subscriptions;
	    int intId = (int) id;
        if (!handlers.TryGetValue(intId, out subscriptions))
			return;

	    for (int i = 0; i < PRIORITY_COUNT; i++)
	    {
	        subscriptions[i].Remove(handler);
	    }
	}

	public static void Send(EventId id, EventInfo info, EventTargetType target = EventTargetType.Local, int specificId = 0)
	{
	    int intId = (int) id;

		if (target == EventTargetType.Local || (target == EventTargetType.ToSpecific && specificId == PhotonNetwork.player.ID)
			|| (target == EventTargetType.ToMaster && BattleConnectManager.IsMasterClient))
		{
			SendLocalEvent(id, info);
			return;
		}

		if (target == EventTargetType.ToAll)
		{
			SendLocalEvent(id, info);
		}

        if (target == EventTargetType.ToAll || target == EventTargetType.ToOthers)
		{
			PhotonNetwork.RaiseEvent(DISPATCHER_EVENT_CODE, new PhotonEventAdapter(intId, info), true, new RaiseEventOptions() { Receivers = ReceiverGroup.Others });
			if (instance.showEventSent && instance.eventsToLog.Contains(id))
				DT.Log("<color=\"green\">Sending Event TO OTHER CLIENTS: ({0}). EventInfo = {1}</color>", intId, info);

			return;
		}

        if (target == EventTargetType.ToMaster || (target == EventTargetType.ToSpecific && specificId == PhotonNetwork.masterClient.ID))
        {
            PhotonNetwork.RaiseEvent(DISPATCHER_EVENT_CODE, new PhotonEventAdapter(intId, info), true,
                                        new RaiseEventOptions() { Receivers = ReceiverGroup.MasterClient });
            return;
        }

        if (target == EventTargetType.ToSpecific)
		{
			PhotonNetwork.RaiseEvent(DISPATCHER_EVENT_CODE, new PhotonEventAdapter(intId, info), true,
										new RaiseEventOptions() { TargetActors = new[] { specificId } });
			if (instance.showEventSent && instance.eventsToLog.Contains(id))
                DT.Log("<color=\"green\">Sending Event TO CLIENT#{3}: ({0}). EventInfo = {1} at {2}</color>", intId, info, specificId, Time.time);
            return;
        }
    }
	
	/* PRIVATE SECTION */
	private static void SendLocalEvent(EventId id, EventInfo info)
	{
        if (instance.showEventSent && instance.eventsToLog.Contains(id))
            DT.Log("<color=\"green\">Sending Event ({0}). EventInfo = {1} at {2}</color>", id, info, Time.time);
	    List<EventSubscribeHandler>[] subscriptions;

        if (!handlers.TryGetValue((int)id, out subscriptions))
            return;

	    foreach (var subscription in subscriptions)
	    {
	        for (int i = 0; i < subscription.Count; i++)
	        {
	            try
	            {
	                subscription[i].Invoke(id, info);
	            }
                catch (Exception e)
                {
                    DT.LogError("Dispatcher event exception ({0}).\n{1}\n{2} ", id, e.Message, e.StackTrace);
                }

	            if (info != null && info.Cancelled)
	                return;
	        }
	    }
	}

	private static void OnPhotonEvent(byte eventCode, object content, int senderId)
	{
		if (eventCode != DISPATCHER_EVENT_CODE)
			return;

		PhotonEventAdapter adapter = (PhotonEventAdapter)content;
		if (instance.showEventSent && instance.eventsToLog.Contains((EventId)adapter.eventId))
			Debug.Log("FROM NETWORK:");
		SendLocalEvent((EventId)adapter.eventId, adapter.eventInfo);
	}
}
