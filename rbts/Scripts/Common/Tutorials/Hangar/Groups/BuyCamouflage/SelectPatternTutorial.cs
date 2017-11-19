using UnityEngine;
using System.Collections;

public class SelectPatternTutorial : GoToPatternShopTutorial
{
    private int selectedPatternCellId;
    private GameObject arrow;

    public new static SelectPatternTutorial Instance { get; private set; }

    public override bool IsActive
    {
        get
        {
            return ProfileInfo.TutorialIndex == index &&
                   GUIPager.ActivePage == page &&
                   !HasCamouflage &&
                   HasEnoughMoneyToBuyCamouflage &&
                   !IsNeededPatternSelected &&
                   !(CanBuySelectedCamouflage && !IsNeededPatternSelected);
        }
    }

    private bool IsNeededPatternSelected
    {
        get { return selectedPatternCellId == NeededPatternCellId; }
    }

    protected override void Awake()
    {
        base.Awake();

        Instance = this;

        Messenger.Subscribe(EventId.PatternSelected, OnShopItemSelected);
        Messenger.Subscribe(EventId.PatternShopFilled, OnPatternShopFilled);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        Instance = null;

        Messenger.Unsubscribe(EventId.PatternSelected, OnShopItemSelected);
        Messenger.Unsubscribe(EventId.PatternShopFilled, OnPatternShopFilled);
    }

    protected override void Init(EventId id = 0, EventInfo info = null)
    {
        if (isInitialized)
            return;

        base.Init(id, info);

        index = (int)Tutorials.buyCamouflage;
        page = TutorialPages.PatternShop.ToString();

        Refresh();
    }

    protected override void InstantiateTutorialParts() { }

    protected override IEnumerator RefreshingRoutine()
    {
        holder.Wrapper.SetActive(IsActive);

        // Вырубаем стрелку отдельно, т.к. она может быть не внутри враппера:
        if (arrow != null)
            arrow.gameObject.SetActive(IsActive);

        yield return new WaitForEndOfFrame();
    }

    private void OnPatternShopFilled(EventId id, EventInfo info)
    {
        TryInstantiateArrow();
        Refresh();
    }

    private void OnShopItemSelected(EventId id, EventInfo info)
    {
        selectedPatternCellId = ((EventInfo_I)info).int1;
        Refresh();
    }

    private void TryInstantiateArrow()
    {
        if (arrow != null) // Очень костыльно, но тогглы хз где могут уничтожиться.
        {
            Destroy(arrow.gameObject);
            arrow = null;
        }

        if (!PatternShop.Selectors.ContainsKey(NeededPatternCellId))
            return;

        tk2dUIToggleControl toggle = PatternShop.Selectors[NeededPatternCellId].toggle;

        arrow
            = InstantiateTutorialPart(
                path:           "Tutorials/ArrowPointerWrapper",
                anchor:         TutorialHolder.CamAnchors.lowerLeft,
                position:       toggle.transform.position,
                yPos:           toggle.transform.position.y + ArrowYOffset,
                eulerAngles:    Vector3.zero,
                partName:       "ArrowPointerWrapper");

        arrow.transform.parent = toggle.transform;
        // Без этого стрелка почему-то в первый раз ставится ниже:
        arrow.transform.SetY(arrow.transform.parent.position.y + ArrowYOffset);
    }
}
