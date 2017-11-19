using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MapFramesCreator : MonoBehaviour
{
    [Header("Порядок кнопок переключения режимов должен совпадать с енамом GameMode!")]
    public MapSelector mapSelectionFramePrefab;
    public GameObject mapsContainer;
    public Transform firstFramePos;
    public float mapFramesOffset = 560;
    public tk2dUIScrollableArea scrollableArea;
    
    [SerializeField] private tk2dUIToggleButtonGroup modeButtonGroup;
    private List<MapSelector> mapSelectionFrames = new List<MapSelector>();//Все плашки карт + рандомная
    private tk2dUIToggleButtonGroup toggleBtnGroup;

    public static MapFramesCreator Instance { get; private set; }
    public static List<MapSelector> MapSelectionFrames { get { return Instance.mapSelectionFrames; } }

    void Awake()
    {
        Instance = this;
        Dispatcher.Subscribe(EventId.AfterHangarInit, AfterHangarInit);
        Dispatcher.Subscribe(EventId.PlayerLevelChanged, OnPlayerLevelChanged);
        Dispatcher.Subscribe(EventId.GameModeChanged, OnGameModeChanged);
        toggleBtnGroup = GetComponent<tk2dUIToggleButtonGroup>();
        modeButtonGroup.OnChange += OnGameModeSelect;
        UpdateGameModeButtons();
    }

    void OnDestroy()
    {
        Instance = null;
        Dispatcher.Unsubscribe(EventId.AfterHangarInit, AfterHangarInit);
        Dispatcher.Unsubscribe(EventId.PlayerLevelChanged, OnPlayerLevelChanged);
        Dispatcher.Unsubscribe(EventId.GameModeChanged, OnGameModeChanged);
        modeButtonGroup.OnChange -= OnGameModeSelect;
    }

    private void AfterHangarInit(EventId id, EventInfo info)
    {
        SetMapsList();//Заполняем список всех плашек
    }

    private void SetMapsList()
    {
        int mapIndex = 0;
        List<MapInfo> mapList = GameData.allMapsDic.Values.OrderBy(key => key.order).ToList();
        if(mapSelectionFrames.Count > 0)
        {
            for (int i = 0; i < mapSelectionFrames.Count; i++)
                if (mapSelectionFrames[i] != null)
                    Destroy(mapSelectionFrames[i].gameObject);

            toggleBtnGroup.AddNewToggleButtons(new tk2dUIToggleButton[0]);
            mapSelectionFrames.Clear();
        }
        
        for (int i = 0; i < mapList.Count; i++)
        {
            if ((mapList[i].id == GameManager.MapId.random_map && GameData.availableMapsDic.Count > 1) || mapList[i].id != GameManager.MapId.random_map)
            {
                mapSelectionFrames.Add(Instantiate(mapSelectionFramePrefab));

                mapSelectionFrames[mapIndex].Init(
                    pos: firstFramePos.localPosition + Vector3.right * mapFramesOffset * mapIndex++,
                    mapId: mapList[i].id,
                    mapLevel: mapList[i].mapLevel,
                    fuelRequired: mapList[i].fuelRequired,
                    isMapEnabled: mapList[i].isEnabled);
            }
        }

        toggleBtnGroup.AddNewToggleButtons(mapSelectionFrames.Select(item => item.UiToggleButton).Cast<tk2dUIToggleButton>().ToArray());
    }

    private void OnPlayerLevelChanged(EventId id, EventInfo info)
    {
        if(HangarController.Instance != null && HangarController.Instance.IsInitialized)
            SetMapsList();
    }

    private void OnGameModeChanged(EventId id, EventInfo info)
    {
        //Debug.LogError("MapFramesCreator.OnGameModeChanged " + GameData.Mode);
        int val = ((int)GameData.Mode) - 1;
        modeButtonGroup.SelectedIndex = Mathf.Clamp(val, 0, val);
        UpdateGameModeButtons();
    }

    private void OnGameModeSelect(tk2dUIToggleButtonGroup buttonGroup)
    {
        GameData.GameMode mode = (GameData.GameMode)buttonGroup.SelectedIndex + 1;
        if (mode != GameData.Mode)
            GameData.Mode = mode;
    }

    private void UpdateGameModeButtons()
    {
        if (GameData.gameModes == null || GameData.gameModes.Count == 0)
            return;
        int activeBtnsCount = 0;
        for (int i = 0; i < modeButtonGroup.ToggleBtns.Length; i++)
        {
            GameData.GameMode btnGameMode = BtnIndexToGameMode(i);
            modeButtonGroup.ToggleBtns[i].gameObject.SetActive(GameData.gameModes.Contains(btnGameMode)  );
            if (GameData.gameModes.Contains(BtnIndexToGameMode(i)))
                activeBtnsCount++;
            
        }
        //Если доступен только один режим - выключаем кнопки выбора режимов
        modeButtonGroup.gameObject.SetActive(activeBtnsCount > 1);
    }

    private GameData.GameMode BtnIndexToGameMode(int btnIndex)
    {
        return (GameData.GameMode)(btnIndex + 1);
    }   
}
