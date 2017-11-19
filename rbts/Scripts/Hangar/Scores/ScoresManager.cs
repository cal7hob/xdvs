using UnityEngine;
using System.Collections.Generic;

public class ScoresManager : MonoBehaviour {
	public GameObject scoresPrefab;
	public GameObject prefabContainer;
	public GameObject scoresBoxWrapper;

    public static ScoresManager Instance { get; private set; }
	private ScoresController m_controller;

	void Awake () {
        Instance = this;
		Messenger.Subscribe(EventId.AfterHangarInit, Init, 0);
	}

	void OnDestroy()
	{
		Messenger.Unsubscribe(EventId.AfterHangarInit, Init);
        Instance = null;
	}

	// Use this for initialization
	void Init (EventId id, EventInfo info) {
		if (prefabContainer.transform.childCount == 0) {
	        scoresBoxWrapper = Instantiate (scoresPrefab);
			scoresBoxWrapper.name = "ScoresBoxWrapper";
			Vector3 pos = scoresBoxWrapper.transform.localPosition;
			scoresBoxWrapper.transform.parent = prefabContainer.transform;
			scoresBoxWrapper.transform.localPosition = pos;
		    scoresBoxWrapper.AddComponent<FriendsManager>();
		}
		m_controller = prefabContainer.transform.Find ("ScoresBoxWrapper").GetComponent<ScoresController> ();
        m_controller.gameObject.SetActive (false);

        SaveScoresToServer();
	}

	//-----------------------------------------------------------------------------------------------------------
	// Запросы
	public void SaveScoresToServer () {
        var req = Http.Manager.Instance ().CreateRequest ("/statistics/setScore");
        req.Form.AddField ("nickName", ProfileInfo.PlayerName);
        if (ProfileInfo.Gold >= GameData.cheatTreshold) {
            req.Form.AddField ("score", 0);
		}
		else {
            req.Form.AddField ("score", ProfileInfo.Experience);
		}
        //StartCoroutine (CallWWW ("/statistics/setScore", data, delegate(WWW result){
        StartCoroutine (req.Call (delegate (Http.Response result) 
            {
#if ENABLE_PROFILER
                UnityEngine.Profiling.Profiler.BeginSample ("ScoresLoad");
#endif
                var d = result.Data;
                if (!d.ContainsKey ("stats")) {
                    Debug.LogWarning ("Not found 'stats' key in Statistics result");
                    return;
                }
                var s = d["stats"] as Dictionary<string, object>;
                if (s == null) {
                    Debug.LogWarning ("Can't convert 'stats' object to Dictionary");
                    return;
                }
                StartCoroutine(m_controller.ScoresReceived (s));
#if ENABLE_PROFILER
                UnityEngine.Profiling.Profiler.EndSample ();
#endif
                FriendsManager.ForceUpdateFriends ();
            }, delegate (Http.Response result) {
                Debug.LogWarning ("Can't send score to our server. Error: " + result.error);
            })
        );
	}

}
