using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HistoryPage
{
    public string PageName { get; private set; }
    public bool AddToHistory { get; private set; }
    public int VoiceEventId { get; private set; }

    public HistoryPage(string pageName, bool addToHistory, int voiceEventId)
	{
        PageName = pageName;
        AddToHistory = addToHistory;
        VoiceEventId = voiceEventId;
	}

    public void Clean()
    {
        VoiceEventId = -1;
    }
}

public class PageOptions
    {
        public bool AddToHistory { get; private set; }
        public int VoiceEventId { get; private set; }

        public PageOptions(bool addToHistory = true, int voiceEventId = -1)
        {
            AddToHistory = addToHistory;
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
        public HangarPage hangarPage;//для страниц которые не инстанируются - указываем HangarPage скрипт, если такой есть
        public bool awakeOnLoad;
        public GameObject[] objects;
	    public GameObject queueablePage;
        [Header("Если страница инстанируется - указать префаб и в какой объект инстанировать")]
        public GameObject prefab;
        public Transform parentForInstantiating;
        [Header("Если allowCamRotationControl == true, allowCamRotation устанавливается в true автоматически")]
        [SerializeField] private bool allowCamRotationControl = false;
        [SerializeField] private bool allowCamRotation = true;
        public bool AllowCamRotationControl { get { return allowCamRotationControl; } }
        public bool AllowCamRotation { get { return allowCamRotationControl || allowCamRotation; } }

        private bool active;

		public bool Active
		{
			get { return active; }
			set
			{
				if (value == active)
					return;

				active = value;
                if (hangarPage)
                    hangarPage.SetActive(active);//Типо можно не накидывать враппер самой страницы в список объектов гуипейджера
                Refresh();
			}
		}

        public int VoiceEventId { get; set; }

        public ParamDict WindowData { get; set; }

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

    public static GUIPager Instance { get; private set; }
    public static Stack<HistoryPage> PagesHistory { get; private set; }
    public static Queue<QueueablePage> PagesQueue { get; private set; }
    public static Queue<Action> ActionsQueue { get; private set; }
    public static HashSet<string> disabledDynamicPages = new HashSet<string>();//список страниц, запрещенных (например серевером) к инстанированию.
    public static event Action<string, string> OnPageChange;

    public Page[] pages;
    [Header("Включать всякие там анкоры и др., случайно выключенные в префабе")]
    public GameObject[] objectsToActivateOnAwake;
    [SerializeField] private bool debug;


    public static Page ActivePage
    {
        get
        {
            if (Instance == null)
                return null;

            return Instance.activePage;
        }
    }
    public static string ActivePageName
    {
        get
        {
            if (Instance == null || Instance.activePage == null)
                return null;

            return Instance.activePage.name;
        }
    }
    public static bool Dbg { get { return Instance != null && Instance.debug; } }

    private Page activePage;
    private string prevPageName;

    public static void EnqueuePage(string pageName, bool addToHistory = true, int voiceEventId = -1)
    {
        var pageToEnqueue = Instance.pages.FirstOrDefault(page => page.name == pageName);

        if (pageToEnqueue == null)
        {
            Debug.LogErrorFormat("Pages doesn't have {0} page", pageName);
            return;
        }

        var queueablePage = new QueueablePage(pageToEnqueue, 
            new PageOptions(addToHistory, voiceEventId));

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

    private void Awake()
	{
		if (pages == null || pages.Length == 0 || Instance != null)
		{
			gameObject.SetActive(false);
			return;
		}

        Instance = this;

        #region Инстанирование динамически создаваемых окон
        for (int i = 0; i < pages.Length; i++)
        {
            if(pages[i].prefab && pages[i].parentForInstantiating && !disabledDynamicPages.Contains(pages[i].name))
            {
                GameObject obj = Instantiate(pages[i].prefab, pages[i].parentForInstantiating);
                obj.transform.localPosition = Vector3.zero;
                obj.name = obj.name.Replace("(Clone)", "");
                pages[i].hangarPage = obj.GetComponent<HangarPage>();
            }
        }
        #endregion

        #region objectsToActivateOnAwake
        if (objectsToActivateOnAwake != null)
            for (int i = 0; i < objectsToActivateOnAwake.Length; i++)
                if (objectsToActivateOnAwake[i] != null)
                    objectsToActivateOnAwake[i].SetActive(true);
        #endregion

        PagesHistory = new Stack<HistoryPage>(5);

        #region  AwakeSomeObjects
        foreach (Page page in pages)
            if (page.awakeOnLoad)
                foreach (GameObject go in page.objects)
                    StartCoroutine(AwakeObject(go));
        #endregion

        #region Hide all pages, except current
        foreach (Page page in pages)
        {
            page.Active = (page.name == ActivePageName);
            if (!page.Active)//Проходимся по всем окнам и выключаем их объекты
                page.Refresh();
        }

        if (!string.IsNullOrEmpty(ActivePageName) && activePage != null)
            activePage.Refresh();//Включаем только на активной странице, если таковая есть
        #endregion

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
            if (string.IsNullOrEmpty(ActivePageName) || ActivePageName != "MainMenu" || MessageBox.IsShown)
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

        SetActivePage(queuedPage.Page.name, queuedPage.Options.AddToHistory, queuedPage.Options.VoiceEventId, null);

        if (queueablePageImpl != null)
            queueablePageImpl.Activated();
    }

    public static void SetActivePage(string pageName, bool addToHistory = true, int voiceEventId = -1, ParamDict windowData = null)
	{
        if (Dbg)
            Debug.LogErrorFormat("SetActivePage to: {0}", pageName);

        if (Instance == null)
        {
            Debug.LogError("GUIPager.SetActivePage(), Instance == null");
            return;
        }

        //Запрещаем повторное открытие страницы которая уже открыта - предотвращает глюки в страницах, которые что то делают в OnPageChange
        if(!string.IsNullOrEmpty(ActivePageName) && ActivePageName == pageName)
            return;

        if (Instance.activePage != null && addToHistory && !string.IsNullOrEmpty(ActivePageName))
            PagesHistory.Push(new HistoryPage(pageName, addToHistory, voiceEventId));

		bool activated = false;

		foreach (Page page in Instance.pages)
		{
		    page.Active = false;
            page.WindowData = null;
        }

		foreach (Page page in Instance.pages)
		{
			if (pageName == page.name)
			{
                #region strongly before page objects activating
                page.WindowData = windowData;
                Instance.activePage = page;
                Instance.activePage.VoiceEventId = voiceEventId;
                #endregion

                page.Active = true;
				activated = true;
			    
                if (page.objects != null)
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

        if (Dbg)
            Debug.LogErrorFormat("OnPageChange({0}, {1})", Instance.prevPageName, ActivePageName);

        OnPageChange(Instance.prevPageName, ActivePageName);
        Instance.prevPageName = ActivePageName;

        if (ActivePageName == "MainMenu")
            ClearHistory();

        Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I(voiceEventId));
        Dispatcher.Send(EventId.PageChanged, new EventInfo_SimpleEvent());

        GoogleAnalyticsWrapper.LogScreen(ActivePageName);
	}

	public static void Back()
	{
        if (PagesHistory.Count == 0)
        {
            ToMainMenu();
            return;
        }

        HistoryPage prevPage = PagesHistory.Pop();

        while (prevPage.PageName == ActivePageName)
        {
            if (PagesHistory.Count == 0)
            {
                ToMainMenu();
                return;
            }
            prevPage = PagesHistory.Pop();
        }

        SetActivePage(prevPage.PageName, prevPage.AddToHistory, prevPage.VoiceEventId);
	}

    public static void ClearHistory()
    {
        PagesHistory.Clear();
    }

    private static IEnumerator AwakeObject(GameObject go)
    {
        if(go.activeInHierarchy)
            yield break;

        go.SetActive(true);

        yield return null;

        go.SetActive(false);
    }

    public static void ToMainMenu()
    {
        SetActivePage("MainMenu");
        if (HangarController.Instance)
            HangarController.Instance.PlaySound(HangarController.Instance.backSound);
    }

    public static bool QueueContainsPage(string pageName)
    {
        return PagesQueue.Any(page => page.Page.name == pageName);
    }
}
