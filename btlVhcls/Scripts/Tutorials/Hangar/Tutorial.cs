using System.Collections;
using UnityEngine;

public abstract class Tutorial : MonoBehaviour
{

    public enum TutorPrefab
    {
        ArrowPointer,
        CharacterFromRes,
        TutorialMessage,
        TutorialHolder,
    }

    protected bool isInitialized;
    protected bool didPlayVoice;
    protected int index;
    protected string page;
    protected TutorialHolder holder;
    protected SpriteFromRes mainCharacterTexture;//not null if this tutor contains character picture

    protected static Tutorial Instance
    {
        get; private set;
    }

    public virtual bool IsActive
    {
        get { return ProfileInfo.TutorialIndex == index && GUIPager.ActivePageName == page; }
    }

    protected virtual bool DidThisBefore // TODO: выяснить, зачем нужно.
    {
        get { return false; }
    }

    protected virtual float CharacterXOffset // Не надо так.
    {
        get
        {
            if (GameData.IsGame(Game.FutureTanks))
                return 147.8416f;

            if (GameData.IsGame(Game.Armada))
                return 0;

            if (GameData.IsGame(Game.MetalForce))
                return 0;

            return 0;
        }
    }

    protected virtual float CharacterYOffset
    {
        get
        {
            if (GameData.IsGame(Game.FutureTanks))
                return -180.0f;

            if (GameData.IsGame(Game.Armada))
                return -440.0f;

            if (GameData.IsGame(Game.MetalForce))
                return -440.0f;

            if (GameData.IsGame(Game.BattleOfWarplanes | Game.WingsOfWar))
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

            if (GameData.IsGame(Game.MetalForce))
                return 126.0f;

            if (GameData.IsGame(Game.BattleOfWarplanes | Game.WingsOfWar))
                return -90.0f;

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

            if (GameData.IsGame(Game.FutureTanks) || GameData.IsGame(Game.SpaceJet))
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

            if (GameData.IsGame(Game.BattleOfWarplanes | Game.WingsOfWar))
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

            if (GameData.IsGame(Game.MetalForce))
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

            if (GameData.IsGame(Game.MetalForce))
                return 320.0f;

            if (GameData.IsGame(Game.BattleOfWarplanes | Game.WingsOfWar))
                return -170.0f;

            return 250.0f;
        }
    }

    protected virtual float BackArrowXOffset
    {
        get
        {
            if (GameData.IsGame(Game.FutureTanks))
                return 146.0f;

            if (GameData.IsGame(Game.ToonWars))
                return 160.0f;

            if (GameData.IsGame(Game.Armada))
                return 100.0f;

            if (GameData.IsGame(Game.MetalForce))
                return 100.0f;

            if (GameData.IsGame(Game.SpaceJet))
                return 300.0f;

            if (GameData.IsGame(Game.BattleOfWarplanes | Game.WingsOfWar))
                return 295.0f;

            return 220.0f;
        }
    }

    protected virtual float BackArrowYOffset
    {
        get
        {
            if (GameData.IsGame(Game.FutureTanks))
                return -141.0f;

            if (GameData.IsGame(Game.ToonWars) || GameData.IsGame(Game.SpaceJet))
                return -155.0f;

            if (GameData.IsGame(Game.Armada))
                return -100.0f;

            if (GameData.IsGame(Game.MetalForce))
                return -100.0f;

            if (GameData.IsGame(Game.BattleOfWarplanes | Game.WingsOfWar))
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

            if (GameData.IsGame(Game.BattleOfWarplanes | Game.WingsOfWar))
                return 370.0f;

            return 350.0f;
        }
    }

    protected virtual float BuyBtnArrowYOffset
    {
        get
        {
            if (GameData.IsGame(Game.BattleOfWarplanes | Game.WingsOfWar))
                return -540.0f;

            return 0;
        }
    }

    protected virtual TutorialHolder.CamAnchors CharacterAnchor
    {
        get
        {
            if (GameData.IsGame(Game.Armada))
                return TutorialHolder.CamAnchors.upperLeft;

            if (GameData.IsGame(Game.MetalForce))
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

        Dispatcher.Subscribe(EventId.AfterHangarInit, Refresh, 4);
        Dispatcher.Subscribe(EventId.TutorialIndexChanged, Refresh, 4);
        Dispatcher.Subscribe(EventId.PageChanged, Refresh, 4);
        Dispatcher.Subscribe(EventId.ProfileInfoLoadedFromServer, Refresh, 4);
        Dispatcher.Subscribe(EventId.TutorialsInitialized, Refresh, 4);
        Dispatcher.Subscribe(EventId.OnLanguageChange, OnLanguageChange);
    }

    protected virtual void Start()
    {
        if (HangarController.Instance.IsInitialized)
            Init();
        else
            Dispatcher.Subscribe(EventId.AfterHangarInit, Init);
    }

    protected virtual void OnDestroy()
    {
        Instance = null;

        Dispatcher.Unsubscribe(EventId.AfterHangarInit, Refresh);
        Dispatcher.Unsubscribe(EventId.TutorialIndexChanged, Refresh);
        Dispatcher.Unsubscribe(EventId.PageChanged, Refresh);
        Dispatcher.Unsubscribe(EventId.ProfileInfoLoadedFromServer, Refresh);
        Dispatcher.Unsubscribe(EventId.TutorialsInitialized, Refresh);
        Dispatcher.Unsubscribe(EventId.OnLanguageChange, OnLanguageChange);

        Dispatcher.Unsubscribe(EventId.AfterHangarInit, Init);
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
        TutorPrefab prefab,
        TutorialHolder.CamAnchors   anchor,
        Vector3                     position,
        float                       yPos,
        Vector3                     eulerAngles,
        string                      partName = null,
        bool                        isLocalizationNeded = false,
        Transform                   parent = null)
    {
        var tutorialPart = Instantiate(Resources.Load<GameObject>(GetHangarPrefabPath(prefab)));

        parent = parent ?? holder.Anchors[(int)anchor].transform;

        tutorialPart.transform.SetParent(
            parent:             parent,
            worldPositionStays: true);

        var itemPosition = parent.InverseTransformPoint(position);

        itemPosition.z = 5;
        itemPosition.y = yPos;

        tutorialPart.transform.localEulerAngles = eulerAngles;
        tutorialPart.transform.localPosition = itemPosition;

        if (prefab == TutorPrefab.CharacterFromRes)
            mainCharacterTexture = tutorialPart.GetComponent<SpriteFromRes>();
        SetupCharacterTexture();

        if (!string.IsNullOrEmpty(partName))
            tutorialPart.name = partName;

        if (isLocalizationNeded)
            tutorialPart.AddComponent(typeof(LabelLocalizationAgent));

        return tutorialPart;
    }

    private void SetCameraToAnchors()
    {
        foreach (var anchor in holder.Anchors)
            anchor.AnchorCamera = GameData.CurSceneGuiCamera;
    }

    protected void Refresh(EventId id = 0, EventInfo info = null)
    {
        StartCoroutine(RefreshingRoutine());
    }

    protected void OnLanguageChange(EventId id = 0, EventInfo info = null)
    {
        SetupCharacterTexture();
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

        Dispatcher.Send(EventId.TutorialIndexChanged, new EventInfo_I(ProfileInfo.TutorialIndex));
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

    public static string GetHangarPrefabPath(TutorPrefab prefab)
    {
        return string.Format("{0}/GuiPrefabs/Hangar/Tutorials/{1}", GameData.CurInterface, prefab);
    }


    //ohuenno costilniy metod. Chtob sdelat' normalno - nuzhno peredelat' vse tutori naher.
    public void SetupCharacterTexture()
    {
        if (!mainCharacterTexture)
            return;
        if (GameData.IsGame(Game.MetalForce | Game.BattleOfWarplanes | Game.WingsOfWar))
        {
            switch(Localizer.Language)
            {
                case Localizer.LocalizationLanguage.Russian: mainCharacterTexture.SetTexture("tutorCharacter"); break;
                default: mainCharacterTexture.SetTexture("tutorCharacterMan"); break;
            }
        }
    }


}
