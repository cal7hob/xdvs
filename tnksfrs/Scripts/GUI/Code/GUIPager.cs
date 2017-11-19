using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public interface IQueueablePage
{
    void BeforeActivation();
    void Activated();
}

public class HistoryPage
{
    public string PageName { get; private set; }
    public bool ShowBlackAlphaLayer { get; private set; }
    public bool AddToHistory { get; private set; }
    public int VoiceEventId { get; private set; }

    public HistoryPage(string pageName, bool addToHistory, bool showBlackAlphaLayer, int voiceEventId)
	{
        PageName = pageName;
        AddToHistory = addToHistory;
        VoiceEventId = voiceEventId;
		ShowBlackAlphaLayer = showBlackAlphaLayer;
	}

    public void Clean()
    {
        ShowBlackAlphaLayer = false;
        VoiceEventId = -1;
    }
}

public class PageOptions
    {
        public bool ShowBlackAlphaLayer { get; private set; }
        public bool AddToHistory { get; private set; }
        public int VoiceEventId { get; private set; }

        public PageOptions(bool addToHistory = true, bool showBlackAlphaLayer = false, int voiceEventId = -1)
        {
            AddToHistory = addToHistory;
            ShowBlackAlphaLayer = showBlackAlphaLayer;
            VoiceEventId = voiceEventId;
        }
    }

public class QueueablePage
{
    public GUIPager.Page Page { get; private set; }
    public PageOptions Options { get; private set; }

    public QueueablePage(GUIPager.Page page, PageOptions options)
    {
        Page = page;
        Options = options;
    }
}

public class GUIPager : MonoBehaviour
{
    [Serializable]
	public class Page
	{
        public string name;
	    public bool awakeOnLoad;
        public GameObject[] objects;
	    public GameObject queueablePage;

		private bool active;

		public bool Active
		{
			get { return active; }
			set
			{
				if (value == active)
					return;

				active = value;
				Refresh();
			}
		}

        public int VoiceEventId { get; set; }

        public void Refresh()
		{
			foreach (GameObject go in objects)
            {
                if (go)
                    go.SetActive(active);
                else
                    DT.LogError("Object in Page {0} is NULL! Check GuiPager on prefab Hangar", name);
            }
				
		}
	}

    private Page activePage;
    private string prevPageName;

	public static event Action<string, string> OnPageChange;
	public Page[] pages;
	public GameObject blackAlphaLayer;
    [Header("Включать всякие там анкоры и др., случайно выключенные в префабе")]
    public GameObject[] objectsToActivateOnAwake;

	private List<GameObject> doNotAddThisPagesToHistory = new List<GameObject>();

    [SerializeField] private bool debug;
    public static bool Dbg
    {
        get { return Instance != null && Instance.debug; }
    }

	public static Stack<HistoryPage> PagesHistory { get; private set; }
    public static Queue<QueueablePage> PagesQueue { get; private set; }
    public static Queue<Action> ActionsQueue { get; private set; }

    public static GUIPager Instance { get; private set; }

    public static void EnqueuePage(string pageName, bool addToHistory = true, bool showBlackAlphaLayer = false, int voiceEventId = -1)
    {
        var pageToEnqueue = Instance.pages.FirstOrDefault(page => page.name == pageName);

        if (pageToEnqueue == null)
        {
            Debug.LogErrorFormat("Pages doesn't have {0} page", pageName);
            return;
        }

        var queueablePage = new QueueablePage(pageToEnqueue, 
            new PageOptions(addToHistory, showBlackAlphaLayer, voiceEventId));

        PagesQueue.Enqueue(queueablePage);

        if (Dbg)
            Debug.LogErrorFormat("Page {0} enqueued", pageToEnqueue.name);
    }

    public static void EnqueueAction(Action action = null)
    {
        if (action != null)
        {
            ActionsQueue.Enqueue(action);

            if (Dbg)
                Debug.LogError("Action enqueued");
        }
    }

    /*   UNITY EVENTS SECTION   */

    private void Awake()
	{
		if (pages == null || pages.Length == 0 || Instance != null)
		{
			gameObject.SetActive(false);
			return;
		}

        if (objectsToActivateOnAwake != null)
            for (int i = 0; i < objectsToActivateOnAwake.Length; i++)
                if (objectsToActivateOnAwake[i] != null)
                    objectsToActivateOnAwake[i].SetActive(true);

        Instance = this;
		PagesHistory = new Stack<HistoryPage>(5);

        AwakeSomeObjects();
		Refresh();

        PagesQueue = new Queue<QueueablePage>();
        ActionsQueue = new Queue<Action>();

        Dispatcher.Subscribe(EventId.OnReadyToStartWindowsQueue, ReadyToStartWindowsQueueHandler);
	}

    private void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.OnReadyToStartWindowsQueue, ReadyToStartWindowsQueueHandler);
        pages = null;
        Instance = null;
    }

    private void ReadyToStartWindowsQueueHandler(EventId id, EventInfo info)
    {
        if (Instance != null)
            Instance.StartCoroutine(ProcessQueues());
    }

    private IEnumerator ProcessQueues()
    {
        if (Dbg)
            Debug.LogError("Started ProcessQueues() coroutine");

        for (;;)
        {
            if (!string.IsNullOrEmpty(ActivePage) && ActivePage != "MainMenu")
            {
                yield return null;
                continue;
            }

            if (PagesQueue.Count > 0)
            {
                var pageToShow = PagesQueue.Dequeue();

                if (Dbg)
                    Debug.LogErrorFormat("Setting active queuedPage: {0}", pageToShow.Page.name);

                SetActivePage(pageToShow);
            }
            else if (ActionsQueue.Count > 0)
            {
                var action = ActionsQueue.Dequeue();

                if (Dbg)
                    Debug.LogErrorFormat("Executing action: {0}", action);

                action.SafeInvoke();
            }

            yield return null;
        }
    }

    /*   PUBLIC SECTION   */

	public static string ActivePage
	{
	    get
	    {
	        if (Instance == null || Instance.activePage == null)
	            return null;

            return Instance.activePage.name;
	    }
	}

    public static void SetActivePage(QueueablePage queuedPage)
    {
        IQueueablePage queueablePageImpl = null;

        if (queuedPage.Page.queueablePage != null)
        {
            queueablePageImpl = queuedPage.Page.queueablePage.GetComponent(typeof(IQueueablePage)) as IQueueablePage;
        }
        else
        {
            // В GUIPager для страницы {0} не установлен компонент, приводящийся к IQueuedPage!
            Debug.LogErrorFormat("IQueuedPage castable component's GO for page {0} is not set in GUIPager!", queuedPage.Page.name);
        }

        if (queueablePageImpl != null)
            queueablePageImpl.BeforeActivation();

        SetActivePage(queuedPage.Page.name, queuedPage.Options.AddToHistory, queuedPage.Options.ShowBlackAlphaLayer, queuedPage.Options.VoiceEventId);

        if (queueablePageImpl != null)
            queueablePageImpl.Activated();
    }

    public static void SetActivePage(string pageName, bool addToHistory = true, bool showBlackAlphaLayer = false, int voiceEventId = -1)
	{
        /*if (Dbg)
            Debug.LogErrorFormat("SetActivePage to: {0}", pageName);*/

        if (Instance == null)
        {
            //Debug.LogError("GUIPager.SetActivePage(), Instance == null");
            return;
        }

        //Запрещаем повторное открытие страницы которая уже открыта - предотвращает глюки в страницах, которые что то делают в OnPageChange
        if(!string.IsNullOrEmpty(ActivePage) && ActivePage == pageName)
            return;

        if (Instance.activePage != null && addToHistory && !string.IsNullOrEmpty(ActivePage) && IsAddToHistoryRequired)
            PagesHistory.Push(new HistoryPage(pageName, addToHistory, showBlackAlphaLayer, voiceEventId));

        SwitchHangarCamOnTouchRotation(pageName);

		bool activated = false;

		foreach (Page page in Instance.pages)
		{
		    page.Active = false;
		}

		foreach (Page page in Instance.pages)
		{
			if (pageName == page.name)
			{
				page.Active = true;
				activated = true;
			    Instance.activePage = page;
			    Instance.activePage.VoiceEventId = voiceEventId;

                if(page.objects != null)
                    for (int i = 0; i < page.objects.Length; i++)
                    {
                        if (page.objects[i] == null)
                            continue;

                        var showHideGUIPage = page.objects[i].GetComponent<ShowHideGUIPage>();
                        if (showHideGUIPage)
                            showHideGUIPage.MoveToDefaultPosition();
                    }

				break;
			}
		}

		if (!activated)
		{
            if(HangarController.Instance != null)
            {
                DT.LogError("There is no page with name {0} in pager. Go to MainMenu", pageName);
                ToMainMenu();
            }
            else
            {
                DT.LogError("There is no page with name {0} in pager. All pages disactivated.", pageName);
            }
			
			return;
		}

        //В армаде и последующих проектах включение BlackAlphaLayer регулируется префабом ангара
        if (!GameData.IsGame(Game.Armada2))
            Instance.blackAlphaLayer.SetActive(showBlackAlphaLayer);

        if (Dbg)
            Debug.LogErrorFormat("OnPageChange({0}, {1})", Instance.prevPageName, ActivePage);

        OnPageChange(Instance.prevPageName, ActivePage);
        Instance.prevPageName = ActivePage;

        if (ActivePage == "MainMenu")
            ClearHistory();

        Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I(voiceEventId));
        Dispatcher.Send(EventId.PageChanged, new EventInfo_SimpleEvent());

        GoogleAnalyticsWrapper.LogScreen(ActivePage);
	}

    private static bool IsAddToHistoryRequired
    {
        get
        {
            return Instance.doNotAddThisPagesToHistory.All(currentPage => currentPage == null || currentPage.name != ActivePage);
        }
    }

	public static void Back()
	{
        if (PagesHistory.Count == 0)
        {
            ToMainMenu();
            return;
        }

        HistoryPage prevPage = PagesHistory.Pop();

        while (prevPage.PageName == ActivePage)
        {
            if (PagesHistory.Count == 0)
            {
                ToMainMenu();
                return;
            }
            prevPage = PagesHistory.Pop();
        }

        HideBlackAlphaLayer();
        SetActivePage(prevPage.PageName, prevPage.AddToHistory, prevPage.ShowBlackAlphaLayer, prevPage.VoiceEventId);
	}

    public static void ClearHistory()
    {
        PagesHistory.Clear();
    }

	/*   PRIVATE SECTION   */
	private void Refresh()
	{
		foreach (Page page in pages)
        {
            page.Active = (page.name == ActivePage);
            if(!page.Active)//Проходимся по всем окнам и выключаем их объекты
                page.Refresh();
        }

        if (!string.IsNullOrEmpty(ActivePage) && activePage != null)
            activePage.Refresh();//Включаем только на активной странице, если таковая есть
    }

    private void AwakeSomeObjects()
    {
        foreach (Page page in pages)
            if(page.awakeOnLoad)
                foreach (GameObject go in page.objects)
                    StartCoroutine(AwakeObject(go));
    }

    private static IEnumerator AwakeObject(GameObject go)
    {
        if(go.activeInHierarchy)
            yield break;

        go.SetActive(true);

        yield return null;

        go.SetActive(false);
    }

	private static void SwitchHangarCamOnTouchRotation(string pageName)
	{
		/*HangarCameraController.CanMoveOnTouch = true;
	    ItemRotationController.EnableMovement = true;

        if (HangarCameraController.Instance != null && HangarCameraController.Instance.nonCamRotationWindows.Any(window => window.name == pageName))
            HangarCameraController.CanMoveOnTouch = false;*/
	}
	/// <summary>
	/// Вернуть затемнение в начальное состояние - под основным интерфейсом
	/// </summary>
	public static void ResetBlackAlphaLayerSortingOrder()
	{
		MoveBlackAlphaLayerToCustomOrder(0);
	}

	/// <summary>
	////Поднять затемнение над всем интерфейс
	/// </summary>
	public static void MoveBlackAlphaLayerOverTheInterface()
	{
		MoveBlackAlphaLayerToCustomOrder(15);
	}

	public static void MoveBlackAlphaLayerToCustomOrder(int customOrder)
	{
		if (Instance == null || Instance.blackAlphaLayer == null || GameData.IsGame(Game.Armada2))
			return;
		Instance.blackAlphaLayer.SetActive(true);
	}

	public static void HideBlackAlphaLayer()
	{
		if (Instance == null || Instance.blackAlphaLayer == null || GameData.IsGame(Game.Armada2))
			return;
		ResetBlackAlphaLayerSortingOrder();
		Instance.blackAlphaLayer.SetActive(false);
	}

    public static void ToMainMenu()
    {
        SetActivePage("MainMenu");
        if (HangarController.Instance)
            HangarController.Instance.PlaySound(HangarController.Instance.backSound);
    }
}
