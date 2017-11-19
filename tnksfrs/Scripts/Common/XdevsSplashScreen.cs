using UnityEngine;
using System.Collections.Generic;

public class XdevsSplashScreen : MonoBehaviour 
{
    [SerializeField] private GameObject wrapper;
	[SerializeField] private Texture2D defaultSplash;
    [SerializeField] private GameObject debugPanelPrefab;
    [SerializeField] private Transform anchorMiddleCenter;
    public Transform waitingIndicatorParent;

    public WaitingIndicatorBase waitingIndicator;

    public static XdevsSplashScreen Instance { get; private set; }
    
	private void Awake()//Если обнулять instance в OnDestroy() то будет ошибка перетирания instance. Тогда уж лучше инстанировать SplashScreen на сцене лоадинг
	{
		if(Instance != null)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject);
	}
	
	public static void SetActive(bool en, bool showLabels = false, string tex = "")
	{
		if(Instance == null)
		{
			DT.LogWarning("SplashScreen.Instance == null!");
			return;
		}
        if (en && IsShowed)//Повторно не включаем, чтобы не регенерировать совет.
            return;
		Instance.wrapper.SetActive(en);
        SetLabelsVisibility(showLabels);
       
        //if (HangarController.Instance != null)
        //    HangarController.Instance.gameObject.SetActive(!en);
    }

	public static bool IsShowed
	{
		get
		{
			if (Instance == null)
				return false;
			return Instance.wrapper.activeSelf;
		}
	}

	public static void SetActiveWaitingIndicator(bool en,Transform parent = null, Vector3? defaultPosition = null)
	{
        //DT.Log("SetActiveWaitingIndicator({0}). Parent = {1}",en, parent == null ? "Default" : parent.name);
        if (Instance == null)
        {
            DT.LogWarning("SetActiveWaitingIndicator(). Instance == null!!!");
            return;
        }
            
        if (Instance.waitingIndicator == null)//if waiting indicator was forgotten on other scene in some transform...
        {
            DT.LogError("SetActiveWaitingIndicator(). Instance.waitingIndicator == null!!!");
            return;
        }
        if(en)
        {
            Instance.waitingIndicator.Show(parent, defaultPosition);
        }
        else
        {
            Instance.waitingIndicator.Hide();
        }

	}

	public static void SetLabelsVisibility(bool en)
	{
		if (Instance == null)
			return;
		
        if (en)
        {
           
        } 
        else
        {
            
        }
	}

    

    public static void InstantiateDebugPanel()
    {
        if (Instance == null || DebugPanel.Instance != null)
            return;

        if (DT.useDebugPanelForLogging)
        {
            GameObject panel = Instantiate (Instance.debugPanelPrefab);
            panel.transform.parent = Instance.anchorMiddleCenter;
            panel.transform.localPosition = Vector3.zero;
        }
    }
}
