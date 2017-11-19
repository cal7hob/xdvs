using System;
using System.IO;
using ExitGames.Client.Photon;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;


public partial class Messenger : MonoBehaviour
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

        private static byte[] eventIdBytes = new byte[4];

        public PhotonEventAdapter(int id, EventInfo info)
		{
			eventId = id;
			eventInfo = info;
		}

		public static short SerializeAdapter(StreamBuffer outStream, object customObject)
        {
			PhotonEventAdapter adapter = customObject as PhotonEventAdapter;
            short size = 4;
            outStream.Write(BitConverter.GetBytes(adapter.eventId), 0, 4);
            size += adapter.eventInfo.Serialize(outStream);
			
            return size;
		}

		public static PhotonEventAdapter DeserializeAdapter(StreamBuffer inStream, short length)
		{
            inStream.Read(eventIdBytes, 0, 4);
            int eventId = BitConverter.ToInt32(eventIdBytes, 0);
			EventInfo eventInfo = EventInfo.CommonDeserialize(eventId, inStream, length);
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
    private static RaiseEventOptionsFactory reoFactory = new RaiseEventOptionsFactory();
	private static Messenger instance;
	
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

        if (target == EventTargetType.Local
            || (target == EventTargetType.ToSpecific && specificId == PhotonNetwork.player.ID)
            || (target == EventTargetType.ToMaster && PhotonNetwork.isMasterClient))
        {
            SendLocalEvent(id, info);
            return;
        }

        if (target == EventTargetType.ToAll)
        {
            SendLocalEvent(id, info);
        }

	    if (!PhotonNetwork.inRoom)
	        return;

        if (target == EventTargetType.ToAll || target == EventTargetType.ToOthers)
		{
			PhotonNetwork.RaiseEvent(DISPATCHER_EVENT_CODE, new PhotonEventAdapter(intId, info), true, reoFactory.GetREO_ToOthers());
			if (instance.showEventSent && instance.eventsToLog.Contains(id))
				DT.Log("<color=\"green\">Sending Event TO OTHER CLIENTS: ({0}). EventInfo = {1}</color>", intId, info);

			return;
		}

        if (target == EventTargetType.ToMaster || (target == EventTargetType.ToSpecific && specificId == PhotonNetwork.masterClient.ID))
        {
            PhotonNetwork.RaiseEvent(DISPATCHER_EVENT_CODE, new PhotonEventAdapter((int)id, info), true,
                                        reoFactory.GetREO_ToMaster());
            return;
        }

        if (target == EventTargetType.ToSpecific)
		{
			PhotonNetwork.RaiseEvent(DISPATCHER_EVENT_CODE, new PhotonEventAdapter(intId, info), true,
										reoFactory.GetREO_ToSpecific(specificId));
			if (instance.showEventSent && instance.eventsToLog.Contains(id))
                DT.Log("<color=\"green\">Sending Event TO CLIENT#{3}: ({0}). EventInfo = {1} at {2}</color>", intId, info, specificId, Time.time);
		}
    }
	
	/* PRIVATE SECTION */
	private static void SendLocalEvent(EventId id, EventInfo info)
	{
        if (instance.showEventSent && instance.eventsToLog.Contains(id))
            Debug.LogFormat("<color=\"green\">Sending Event ({0}). EventInfo = {1} at {2}</color>", id, info, Time.time);

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
                    Debug.LogErrorFormat("Dispatcher event exception ({0}).\n{1}\n{2} ", id, e.Message, e.StackTrace);
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
