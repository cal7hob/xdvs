using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

public class MapSelectionScrollbar : MonoBehaviour
{
    [SerializeField] private float mapInCenterOffset = 20;
    [SerializeField] private float scrollingSpeed = 2;

    private bool isCentered;
    private float halfScreenWidth;
    private IEnumerator movingSelectedMapToCenter;

    public tk2dUIScrollableArea ScrollableArea { get; private set; }

    public static MapSelectionScrollbar Instance { get; private set; }

    public MapSelector SelectedMap { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        Instance = null;
    }

    public void OnDisable()
    {
        isCentered = false;
    }

    void Start()
    {
        ScrollableArea = transform.GetComponent<tk2dUIScrollableArea>();
        halfScreenWidth = tk2dCamera.Instance.nativeResolutionWidth * 0.5f;
        ScrollableArea.ContentLength = ScrollableArea.MeasureContentLength() + halfScreenWidth;

        SelectedMap = MapFramesCreator.MapSelectionFrames[0];

        foreach (var map in MapFramesCreator.MapSelectionFrames.Where(map => (int)map.MapId == ProfileInfo.lastMapId).Where(map => map.IsMapAvailableForPlay))
        {
            SelectedMap = map;
        }

        movingSelectedMapToCenter = MovingSelectedItemToCenter();
        StartCoroutine(movingSelectedMapToCenter);
    }

    private IEnumerator MovingSelectedItemToCenter()
    {
        var offset = halfScreenWidth - SelectedMap.UiItem.transform.position.x;
        var offsetAbs = Math.Abs(offset);
        

        while (offsetAbs > mapInCenterOffset)
        {
            offset = halfScreenWidth - SelectedMap.UiItem.transform.position.x;
            var dir = Mathf.Sign(offset) > 0 ? -1 : 1;
            offsetAbs = Math.Abs(offset);
            ScrollableArea.Value += scrollingSpeed * Time.smoothDeltaTime * dir * (offsetAbs / halfScreenWidth);
            yield return null;
        }

        SelectedMap.UiToggleButton.IsOn = true;
        isCentered = true;
    }

    public void SelectedItemToCenter(tk2dUIItem uiItem)
    {
        SelectedMap = uiItem.GetComponent<MapSelector>();
        Debug.LogFormat("SelectedItemToCenter {0}", SelectedMap.MapId);
        if (isCentered && Math.Abs(halfScreenWidth - SelectedMap.UiItem.transform.position.x) < mapInCenterOffset)
        {
            if(movingSelectedMapToCenter != null)
                StopCoroutine(movingSelectedMapToCenter);
            ProfileInfo.lastMapId = (int)SelectedMap.MapId;
            if (GameData.IsGame(Game.Armada | Game.IronTanks | Game.FutureTanks | Game.SpaceJet | Game.MetalForce | Game.BattleOfWarplanes | Game.WingsOfWar | Game.ToonWars) && GameData.isConsumableEnabled)
                GUIPager.SetActivePage("ConsumablesPage");
            else
                HangarController.Instance.EnterBattle(SelectedMap.MapId);
                
            return;
        }

        if (!ScrollableArea.gameObject.activeInHierarchy)
        {
            return;
        }

        StopCoroutine(movingSelectedMapToCenter);
        movingSelectedMapToCenter = MovingSelectedItemToCenter();
        StartCoroutine(movingSelectedMapToCenter);
    }
}
