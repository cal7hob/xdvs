using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class CallbackPool : MonoSingleton<CallbackPool> {
	public bool initializeAtStart = false;
	public int initPoolSize=5;
	public int currentPoolSize;
	public bool warningOnCallbackExceedLimit = true;
	public int callbackLimit = 10;
	
	Queue<Callback> callbackQueue;
	List <Callback> disposableCallbacks;
	List <Callback> permanentCallback;
	Dictionary<long,Callback> callbackDict;
	void Start () {
		if (initializeAtStart)
			initialize();
	}
	
	
	public Callback getCallback(CallbackType callbackType){
		Callback callback;
		if (callbackQueue.Count>0){
			callback = callbackQueue.Dequeue();	
			callback.reset();
		} else {			
			callback = createNewCallback(); 
		}
		callback.type = callbackType;
		switch (callbackType){
		case CallbackType.DISPOSABLE:
			disposableCallbacks.Add(callback);
			break;
		case CallbackType.PERMANENT:
			permanentCallback.Add(callback);
			break;
		default:
			Debug.Log("Wrong callbackType" + callbackType.ToString());			
			break;
		}
		
		return callback; 
	}
	
	public void callbackHandler(string resultString){		
		Debug.Log("callbackHandler fired with resutlString: \n"+resultString);
		Dictionary<string,object> resultObj=MiniJSON.Json.Deserialize(resultString) as Dictionary<string,object>;
		long    callbackId = (long) resultObj["id"];
		Debug.Log("callbackId="+callbackId);
		object result     = resultObj.ContainsKey("object") ? resultObj["object"] : null;
		Callback callback = callbackDict[callbackId];
		if (callback.action!=null){
			callback.action(result, callback);
		} else {
			Debug.Log("callback "+callbackId+" is empty");
		}
		
		if (callback.type == CallbackType.DISPOSABLE){			
			disposableCallbacks.Remove(callback);
			enqueCallback(callback);
		}		
	}
	
	void enqueCallback(Callback callback){
		callback.reset();
		callbackQueue.Enqueue(callback);
	}
	
	public void releasePermanentCallback(Callback callback)
    {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        Debug.Log("releasing callback id="+callback.id+"   mruListenerId="+callback.mailruEventId);
		if (permanentCallback.Contains(callback)){
			if (callback.mailruEventId != Callback.UNSET_HTML_EVENT_ID){				
				string eval="mailru.events.remove(CALLBACK_ID);"
									.Replace("CALLBACK_ID",""+callback.mailruEventId);
				Debug.Log("releasing callback js:\n"+eval);
				Application.ExternalEval(eval);	
			} else {
				Debug.LogError("something wrong callback id="+callback.id+" permanent but doesnt have htmlEventId to unsubscribe it from mail ru events");
			}			
			enqueCallback(callback);			
			permanentCallback.Remove(callback);
		} else {
			Debug.Log("permanent callback doesn't contain callback with id= "+callback.id);
		}
#endif
	}
	
	public Callback createNewCallback ()
	{
		currentPoolSize++;
		if (warningOnCallbackExceedLimit && currentPoolSize>=callbackLimit){
			Debug.LogWarning("total amount of callback is "+currentPoolSize+"\n" +
				" probably you forget to destroy some permanent callback \n" +
				" if you want to increase amount of callback limit change [callbackLimit] in  CallbackPool \n" +
				" If you want to disable this check, please make  [warningOnCallbackExceedLimit] = false");
		}
		Callback c = new Callback();
		callbackDict.Add(c.id,c);
		return c;		
	}
	
	public void setMailruEventId(string parameters){
		Debug.Log("setMailruEventId params="+parameters);
		 Dictionary<string,object> result=MiniJSON.Json.Deserialize(parameters) as Dictionary<string,object>;
		long callbackId=(long)result["callbackId"];
		long mailruEventId=(long)result["mailruEventId"];
		callbackDict[callbackId].mailruEventId=mailruEventId;		
	}
	
	
#region initialization		
	public void initialize(){
		initHtmlJS ();
		initCallbackQueue();
	}	
	
	void initCallbackQueue(){
		if (initPoolSize>0){
			callbackDict        = new Dictionary<long, Callback>();
			callbackQueue       = new Queue          <Callback>();
			disposableCallbacks = new List           <Callback>();
			permanentCallback   = new List           <Callback>();
			currentPoolSize = 0;
			int i = initPoolSize;			
			while (i-- > 0){
				Callback c = createNewCallback ();
				callbackQueue.Enqueue(c);
			}
		} else {
			Debug.LogError("pool size must be greater than 0");
		}
	}
	
	void initHtmlJS ()
    {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        string commandStringify = @"
			JSON.stringify = JSON.stringify || function (obj) {
			    var t = typeof (obj);
			    if (t != ""object"" || obj === null) {
			        // simple data type
			        if (t == ""string"") obj = '""'+obj+'""';
			        return String(obj);
			    }
			    else {
			        // recurse array or object
			        var n, v, json = [], arr = (obj && obj.constructor == Array);
			        for (n in obj) {
			            v = obj[n]; t = typeof(v);
			            if (t == ""string"") v = '""'+v+'""';
			            else if (t == ""object"" && v !== null) v = JSON.stringify(v);
			            json.push((arr ? """" : '""' + n + '"":') + String(v));
			        }
			        return (arr ? ""["" : ""{"") + String(json) + (arr ? ""]"" : ""}"");
			    }
			};			
		";
	
		string commandCallback = @"
			container.callback = function (id, obj){
				var result=new Object();
				result.id=id;
				result.object=obj;
				var resultString=JSON.stringify(result);				
				SendMessage('OBJECT_NAME','callbackHandler',resultString);				
			}"
#if UNITY_WEBPLAYER
            .Replace("SendMessage", "u.getUnity().SendMessage")
#endif
            .Replace("OBJECT_NAME",gameObject.name);	
		
		string commandUpdateCallbackId = @"
			container.updateCallbackId = function (callbackId, mailruEventId){
				var result=new Object();
				result.callbackId=callbackId;
				result.mailruEventId=mailruEventId;
				var resultString=JSON.stringify(result);
				console.log('sending ids to callback'+callbackId+'  mruId'+mailruEventId);
				SendMessage('OBJECT_NAME','setMailruEventId',resultString);
			}"
#if UNITY_WEBPLAYER
            .Replace("SendMessage", "u.getUnity().SendMessage")
#endif
            .Replace("OBJECT_NAME",gameObject.name);
				
		Application.ExternalEval(commandStringify);
		Application.ExternalEval(commandCallback);
		Application.ExternalEval(commandUpdateCallbackId);
#endif
	}
#endregion	
	
	
}
