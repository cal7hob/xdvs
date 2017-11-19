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

    protected virtual bool DidThisBefore // TODO: выяснить, зачем нужно.
    {
        get { return false; }
    }

    protected virtual float CharacterXOffset // Не надо так.
    {
        get
        {
            if (GameData.IsGame(Game.FutureTanks) || GameData.IsGame(Game.FTRobotsInvasion))
                return 147.8416f;

            if (GameData.IsGame(Game.Armada))
                return 0;

            return 0;
        }
    }

    protected virtual float CharacterYOffset
    {
        get
        {
            if (GameData.IsGame(Game.FutureTanks) || GameData.IsGame(Game.FTRobotsInvasion))
                return -180.0f;

            if (GameData.IsGame(Game.Armada))
                return -440.0f;

            if (GameData.IsGame(Game.BattleOfWarplanes))
                return -540.0f;

            return 0;
        }
    }

    protected virtual float MessageXOffset
    {
        get
        {
            if (GameData.IsGame(Game.IronTanks))
            {
                switch (ScreenExtensions.Aspect)
                {
                    case ScreenExtensions.AspectRatio.SixteenToNine:
                        return 125.0f;

                    case ScreenExtensions.AspectRatio.SixteenToTen:
                        return 125.0f;

                    case ScreenExtensions.AspectRatio.FourToThree:
                        return 20.0f;
                }
            }

            if (GameData.IsGame(Game.FutureTanks))
                return 138.0f;

            if (GameData.IsGame(Game.ToonWars))
                return -27.0f;

            if (GameData.IsGame(Game.Armada))
                return 126.0f;

            if (GameData.IsGame(Game.BattleOfWarplanes))
                return -120.0f;

            return 0;
        }
    }

    protected virtual float MessageYOffset
    {
        get
        {
            if (GameData.IsGame(Game.IronTanks))
            {
                switch (ScreenExtensions.Aspect)
                {
                    case ScreenExtensions.AspectRatio.SixteenToNine:
                        return -125.0f;

                    case ScreenExtensions.AspectRatio.SixteenToTen:
                        return -30.0f;

                    case ScreenExtensions.AspectRatio.FourToThree:
                        return 130.0f;
                }
            }

            if (GameData.IsGame(Game.FutureTanks) || GameData.IsGame(Game.SpaceJet)
                || GameData.IsGame(Game.FTRobotsInvasion))
            {
                switch (ScreenExtensions.Aspect)
                {
                    case ScreenExtensions.AspectRatio.SixteenToNine:
                        return -204.0f;

                    case ScreenExtensions.AspectRatio.SixteenToTen:
                        return -84.0f;

                    case ScreenExtensions.AspectRatio.FourToThree:
                        return 36.0f;
                }
            }

            if (GameData.IsGame(Game.BattleOfWarplanes))
            {
                switch (ScreenExtensions.Aspect)
                {
                    case ScreenExtensions.AspectRatio.SixteenToNine:
                        return -70.0f;

                    case ScreenExtensions.AspectRatio.SixteenToTen:
                        return -30.0f;

                    case ScreenExtensions.AspectRatio.FourToThree:
                        return 100.0f;
                }
            }

            if (GameData.IsGame(Game.ToonWars))
                return 75.0f;

            if (GameData.IsGame(Game.Armada))
                return -20.0f;

            return 0;
        }
    }

    protected virtual float ArrowYOffset
    {
        get
        {
            if (GameData.IsGame(Game.ToonWars) || GameData.IsGame(Game.SpaceJet))
                return 380.0f;

            if (GameData.IsGame(Game.Armada))
                return 320.0f;

            if (GameData.IsGame(Game.BattleOfWarplanes))
                return -170.0f;

            return 250.0f;
        }
    }

    protected virtual float BackArrowXOffset
    {
        get
        {
            if (GameData.IsGame(Game.FutureTanks) || GameData.IsGame(Game.FTRobotsInvasion))
                return 146.0f;

            if (GameData.IsGame(Game.ToonWars))
                return 160.0f;

            if (GameData.IsGame(Game.Armada))
                return 100.0f;

            if (GameData.IsGame(Game.SpaceJet))
                return 300.0f;

            if (GameData.IsGame(Game.BattleOfWarplanes))
                return 295.0f;

            return 220.0f;
        }
    }

    protected virtual float BackArrowYOffset
    {
        get
        {
            if (GameData.IsGame(Game.FutureTanks) || GameData.IsGame(Game.FTRobotsInvasion))
                return -141.0f;

            if (GameData.IsGame(Game.ToonWars) || GameData.IsGame(Game.SpaceJet))
                return -155.0f;

            if (GameData.IsGame(Game.Armada))
                return -100.0f;

            if (GameData.IsGame(Game.BattleOfWarplanes))
                return -662.0f;

            return -215.0f;
        }
    }

    protected virtual float BuyBtnArrowXOffset
    {
        get
        {
            if (GameData.IsGame(Game.SpaceJet))
                return 400.0f;

            if (GameData.IsGame(Game.BattleOfWarplanes))
                return 370.0f;

            if (GameData.IsGame(Game.FTRobotsInvasion))
                return -470.0f;

            return 350.0f;
        }
    }

    protected virtual float BuyBtnArrowYOffset
    {
        get
        {
            if (GameData.IsGame(Game.BattleOfWarplanes))
                return -540.0f;

            if (GameData.IsGame(Game.FutureTanks))
                return 105.0f;

            if (GameData.IsGame(Game.FTRobotsInvasion))
                return 58.75f;

            return 0;
        }
    }

    protected virtual float RentBtnArrowYOffset
    {
        get
        {
            return 0;
        }
    }

    protected virtual string TutorialMessagePath
    {
        get
        {
            if (GameData.IsGame(Game.Armada))
                return "Tutorials/tutorialMessage_x_AR";

            return "Tutorials/tutorialMessage_x";
        }
    }

    protected virtual TutorialHolder.CamAnchors CharacterAnchor
    {
        get
        {
            if (GameData.IsGame(Game.Armada))
                return TutorialHolder.CamAnchors.upperLeft;

            return TutorialHolder.CamAnchors.lowerLeft;
        }
    }

    protected virtual TutorialHolder.CamAnchors TutorialMessageAnchor
    {
        get
        {
            return TutorialHolder.CamAnchors.middleLeft;
        }
    }

    protected virtual void Awake()
    {
        holder = GetComponent<TutorialHolder>();

        Instance = this;

        Messenger.Subscribe(EventId.AfterHangarInit, Refresh, 4);
        Messenger.Subscribe(EventId.TutorialIndexChanged, Refresh, 4);
        Messenger.Subscribe(EventId.PageChanged, Refresh, 4);
        Messenger.Subscribe(EventId.ProfileInfoLoadedFromServer, Refresh, 4);
        Messenger.Subscribe(EventId.TutorialsInitialized, Refresh, 4);       
    }

    protected virtual void Start()
    {
        if (HangarController.Instance.IsInitialized)
            Init();
        else
            Messenger.Subscribe(EventId.AfterHangarInit, Init);
    }

    protected virtual void OnDestroy()
    {
        Instance = null;

        Messenger.Unsubscribe(EventId.AfterHangarInit, Refresh);
        Messenger.Unsubscribe(EventId.TutorialIndexChanged, Refresh);
        Messenger.Unsubscribe(EventId.PageChanged, Refresh);
        Messenger.Unsubscribe(EventId.ProfileInfoLoadedFromServer, Refresh);
        Messenger.Unsubscribe(EventId.TutorialsInitialized, Refresh);

        Messenger.Unsubscribe(EventId.AfterHangarInit, Init);
    }

    protected virtual void Init(EventId id = 0, EventInfo info = null)
    {
        if (isInitialized)
            return;

        name = this.GetType().Name;

        InstantiateTutorialParts();
        SetCameraToAnchors();

        isInitialized = true;
    }

    protected abstract void InstantiateTutorialParts();

    protected GameObject InstantiateTutorialPart(
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

        var itemPosition = position;

        itemPosition.z = 5;
        itemPosition.y = yPos;

        tutorialPart.transform.localEulerAngles = eulerAngles;
        tutorialPart.transform.localPosition = itemPosition;

        if (!string.IsNullOrEmpty(partName))
            tutorialPart.name = partName;

        if (isLocalizationNeded)
            tutorialPart.AddComponent(typeof(LabelLocalizationAgent));

        return tutorialPart;
    }

    private void SetCameraToAnchors()
    {
        var cam2D = tk2dCamera.Instance.GetComponent<Camera>();

        foreach (var anchor in holder.Anchors)
            anchor.AnchorCamera = cam2D;
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
            return;

        Destroy(TutorialsController.Instance.tutorialGroups[ProfileInfo.TutorialIndex]);
        Destroy(gameObject);

        ProfileInfo.accomplishedTutorials[(Tutorials) ProfileInfo.TutorialIndex] = true; 

        Messenger.Send(EventId.TutorialIndexChanged, new EventInfo_I(ProfileInfo.TutorialIndex));
        ProfileInfo.SaveToServer();
    }

    protected float GetYPosCorrection() // Костыль, т.к. анкоры иногда тупят.
    {
        return HelpTools.Approximately(Camera.main.aspect, 4.0f / 3.0f, 0.1f)
            ? 180
            : HelpTools.Approximately(Camera.main.aspect, 16.0f / 10.0f, 0.1f)
                ? 60
                : 0;
    }

    public void SetParams(string page, int index)
    {
        this.page = page;
        this.index = index;
    }
}
