using System.Collections;
using UnityEngine;

public abstract class Tutorial : MonoBehaviour
{
    protected bool isInitialized;
    protected bool didPlayVoice;
    protected int index;
    protected string page;
    protected TutorialHolder holder;

    protected static Tutorial Instance
    {
        get; private set;
    }

    public virtual bool IsActive
    {
        get { return ProfileInfo.TutorialIndex == index && GUIPager.ActivePage == page; }
    }

    protected virtual float CharacterXOffset // Не надо так.
    {
        get
        {
            return 0;
        }
    }

    protected virtual float CharacterYOffset
    {
        get
        {
            return -440.0f;
        }
    }

    protected virtual float MessageXOffset
    {
        get
        {
            return 126.0f;
        }
    }

    protected virtual float MessageYOffset
    {
        get
        {
            return -20.0f;
        }
    }

    protected virtual float ArrowYOffset
    {
        get
        {
            return 320.0f;
        }
    }

    protected virtual float BackArrowXOffset
    {
        get
        {
            return 100.0f;
        }
    }

    protected virtual float BackArrowYOffset
    {
        get
        {
            return -100.0f;
        }
    }

    protected virtual float BuyBtnArrowXOffset
    {
        get
        {
            return 350.0f;
        }
    }

    protected virtual float BuyBtnArrowYOffset
    {
        get
        {
            return 0;
        }
    }

    protected string TutorialMessagePath
    {
        get
        {
            return "Tutorials/tutorialMessage_x_AR";
        }
    }

    protected virtual TutorialHolder.CamAnchors CharacterAnchor
    {
        get
        {
            return TutorialHolder.CamAnchors.upperLeft;
        }
    }

    protected virtual TutorialHolder.CamAnchors TutorialMessageAnchor
    {
        get
        {
            return TutorialHolder.CamAnchors.middleLeft;
        }
    }

    public static int RecentlyFinishedTutorialIndex { get; protected set; }

    protected virtual void Awake()
    {
        holder = GetComponent<TutorialHolder>();

        Instance = this;

        Dispatcher.Subscribe(EventId.AfterHangarInit, Refresh, 4);
        Dispatcher.Subscribe(EventId.TutorialIndexChanged, Refresh, 4);
        Dispatcher.Subscribe(EventId.PageChanged, Refresh, 4);
        Dispatcher.Subscribe(EventId.ProfileInfoLoadedFromServer, Refresh, 4);
        Dispatcher.Subscribe(EventId.TutorialsInitialized, Refresh, 4);
        Dispatcher.Subscribe(EventId.CamouflageBought, OnCamoBought);
        Dispatcher.Subscribe(EventId.ModuleReceived, OnModuleReceived);
        Dispatcher.Subscribe(EventId.VehicleBought, OnVehicleBought);
    }

    protected virtual void Start()
    {
        if (HangarController.Instance.IsInitialized)
        {
            Init();
        }
        else
        {
            Dispatcher.Subscribe(EventId.AfterHangarInit, Init);
        }
    }

    protected virtual void OnDestroy()
    {
        Instance = null;

        Dispatcher.Unsubscribe(EventId.AfterHangarInit, Refresh);
        Dispatcher.Unsubscribe(EventId.TutorialIndexChanged, Refresh);
        Dispatcher.Unsubscribe(EventId.PageChanged, Refresh);
        Dispatcher.Unsubscribe(EventId.ProfileInfoLoadedFromServer, Refresh);
        Dispatcher.Unsubscribe(EventId.TutorialsInitialized, Refresh);
        Dispatcher.Unsubscribe(EventId.CamouflageBought, OnCamoBought);
        Dispatcher.Unsubscribe(EventId.ModuleReceived, OnModuleReceived);
        Dispatcher.Unsubscribe(EventId.VehicleBought, OnVehicleBought);
    }

    protected virtual void Init(EventId id = 0, EventInfo info = null)
    {
        if (isInitialized)
        {
            return;
        }

        name = this.GetType().Name;

        InstantiateTutorialParts();
        SetCameraToAnchors();

        isInitialized = true;
    }

    protected abstract void InstantiateTutorialParts();

  /*  public GameObject InstantiateTutorialPart(
        string                      path,
        TutorialHolder.CamAnchors   anchor,
        Vector3                     position,
        float                       yPos,
        Vector3                     eulerAngles,
        string                      partName = null,
        bool                        isLocalizationNeded = false,
        Transform                   parent = null)
    {
        var tutorialPrefab = Resources.Load<TutorialSprite>(path);
        tutorialPrefab.Initialize();

        var tutorialPart = Instantiate(tutorialPrefab.gameObject);

        parent = parent ?? holder.Anchors[(int)anchor].transform;

        tutorialPart.transform.SetParent(
            parent:             parent,
            worldPositionStays: true);

        var itemPosition = parent.InverseTransformPoint(position);

        itemPosition.z = 5;
        itemPosition.y = yPos;

        tutorialPart.transform.localEulerAngles = eulerAngles;
        tutorialPart.transform.localPosition = itemPosition;

        if (!string.IsNullOrEmpty(partName))
        {
            tutorialPart.name = partName;
        }

        if (isLocalizationNeded)
        {
            tutorialPart.AddComponent(typeof(LabelLocalizationAgent));
        }

        return tutorialPart;
    }*/

    private void SetCameraToAnchors()
    {
        var cam2D = tk2dCamera.Instance.GetComponent<Camera>();

        foreach (var anchor in holder.Anchors)
        {
            anchor.AnchorCamera = cam2D;
        }
    }

    protected void Refresh(EventId id = 0, EventInfo info = null)
    {
        StartCoroutine(RefreshingRoutine());
    }

    protected virtual IEnumerator RefreshingRoutine()
    {
        holder.Wrapper.SetActive(IsActive);
        yield return new WaitForEndOfFrame();
    }

    protected virtual void CloseTutorial(EventId id = 0, EventInfo info = null)
    {
        if (ProfileInfo.TutorialIndex != index)
        {
            return;
        }

        Destroy(TutorialsController.Instance.tutorialGroups[ProfileInfo.TutorialIndex]);
        Destroy(gameObject);
        ProfileInfo.accomplishedTutorials[(Tutorials) ProfileInfo.TutorialIndex] = true;
        Dispatcher.Send(EventId.TutorialIndexChanged, new EventInfo_I(ProfileInfo.TutorialIndex));
        
        ProfileInfo.SaveToServer();
    }

    protected float GetYPosCorrection() // Костыль, т.к. анкоры иногда тупят.
    {
        return HelpTools.Approximately(Camera.main.aspect, 4.0f / 3.0f, 0.1f)? 
            180: HelpTools.Approximately(Camera.main.aspect, 16.0f / 10.0f, 0.1f)? 60: 0;
    }

    public void SetParams(string page, int index)
    {
        this.page = page;
        this.index = index;
    }

    private static void OnCamoBought(EventId id, EventInfo info)
    {
        ProfileInfo.accomplishedTutorials[Tutorials.buyCamouflage] = true;
        RecentlyFinishedTutorialIndex = (int)Tutorials.buyCamouflage;
        ProfileInfo.SaveToServer();
    }

    public static void OnModuleReceived(EventId id = 0, EventInfo info = null)
    {
        ProfileInfo.accomplishedTutorials[Tutorials.vehicleUpgrade] = true;
        RecentlyFinishedTutorialIndex = (int)Tutorials.vehicleUpgrade;
        ProfileInfo.SaveToServer();
    }

    private static void OnVehicleBought(EventId id, EventInfo info)
    {
        ProfileInfo.accomplishedTutorials[Tutorials.buyVehicle] = true;
        RecentlyFinishedTutorialIndex = (int)Tutorials.buyVehicle;
        ProfileInfo.SaveToServer();
    }
}
