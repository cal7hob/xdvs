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

    [SerializeField]
    private tk2dUIToggleButtonGroup modeButtonGroup;
    private List<MapSelector> mapSelectionFrames = new List<MapSelector>();//Все плашки карт + рандомная
    private List<GameManager.MapId> availableMaps = new List<GameManager.MapId>();//только доступные для игры карты 
    private tk2dUIToggleButtonGroup toggleBtnGroup;

    public static MapFramesCreator Instance { get; private set; }
    public static List<MapSelector> MapSelectionFrames { get { return Instance.mapSelectionFrames; } }
    public static List<GameManager.MapId> AvailableMaps { get { return Instance.availableMaps; } }

    void Awake()
    {
        Instance = this;
        Dispatcher.Subscribe(EventId.AfterHangarInit, CreateMapSelectionFrames);
        Dispatcher.Subscribe(EventId.PlayerLevelChanged, OnPlayerLevelChanged);
        Dispatcher.Subscribe(EventId.GameModeChanged, OnGameModeChanged);
        toggleBtnGroup = GetComponent<tk2dUIToggleButtonGroup>();
        modeButtonGroup.OnChange += OnGameModeSelect;
        UpdateGameModeButtons();
    }

    void OnDestroy()
    {
        Instance = null;
        Dispatcher.Unsubscribe(EventId.AfterHangarInit, CreateMapSelectionFrames);
        Dispatcher.Unsubscribe(EventId.PlayerLevelChanged, OnPlayerLevelChanged);
        Dispatcher.Unsubscribe(EventId.GameModeChanged, OnGameModeChanged);
        modeButtonGroup.OnChange -= OnGameModeSelect;
    }

    public static GameManager.MapId GetTutorialMap()
    {
        foreach (Dictionary<string, object> obj in GameData.mapsList)
            if ((bool)obj["tutorMap"])
                return (GameManager.MapId)Convert.ToInt32(obj["mapId"]);

        throw new KeyNotFoundException("Tutorial map not selected in admin panel");
    }

    private void CreateMapSelectionFrames(EventId id, EventInfo info)
    {
        //Заполняем доступные карты
        SetAvailableMapsList();

        //Заполняем список всех плашек
        SetMapsList();

        toggleBtnGroup.AddNewToggleButtons(mapSelectionFrames.Select(item => item.UiToggleButton).Cast<tk2dUIToggleButton>().ToArray());
    }

    private void SetMapsList(bool instantiateFrames = true)
    {
        var mapIndex = 0;

        foreach (Dictionary<string, object> map in GameData.mapsList)
        {
            GameManager.MapId mapId = (GameManager.MapId)Convert.ToInt32(map["mapId"]);
            //Инстанируем плашку рандомной карты только если больше одной карты доступно
            if ((mapId == GameManager.MapId.random_map && availableMaps.Count > 1) || mapId != GameManager.MapId.random_map)
            {
                if (instantiateFrames)
                    mapSelectionFrames.Add(Instantiate(mapSelectionFramePrefab));

                mapSelectionFrames[mapIndex].Init(
                    pos: firstFramePos.localPosition + Vector3.right * mapFramesOffset * mapIndex++,
                    mapId: mapId,
                    mapLevel: Convert.ToInt32(map["mapLevel"]),
                    fuelRequired: Convert.ToInt32(map["fuelRequired"]),
                    isMapEnabled: (bool)map["isEnabled"]);
            }
        }
    }

    private void SetAvailableMapsList()
    {
        availableMaps.Clear();

        foreach (Dictionary<string, object> map in GameData.mapsList)
        {
            if ((GameManager.MapId)Convert.ToInt32(map["mapId"]) != GameManager.MapId.random_map &&
                (bool)map["isEnabled"] &&
                Convert.ToInt32(map["mapLevel"]) <= ProfileInfo.Level)
            {
                availableMaps.Add((GameManager.MapId)Convert.ToInt32(map["mapId"]));
            }
        }
    }

    public static MapSelector GetMapItemById(GameManager.MapId _mapId)
    {
        if (Instance == null)
        {
            Debug.LogError("GetMapItemById(). Instance == null!");
            return null;
        }

        if (MapSelectionFrames == null)
        {
            Debug.LogError("GetMapItemById(). MapSelectionFrames == null!");
            return null;
        }

        for (int i = 0; i < MapSelectionFrames.Count; i++)
        {
            if (MapSelectionFrames[i].MapId == _mapId)
            {
                return MapSelectionFrames[i];
            }
        }

        Debug.LogErrorFormat("GetMapItemById(). Not Found mapId {0}", _mapId);
        return null;
    }

    private void OnPlayerLevelChanged(EventId id, EventInfo info)
    {
        if (HangarController.Instance != null && HangarController.Instance.IsInitialized)
        {
            SetMapsList(instantiateFrames: false);
        }
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
        {
            GameData.Mode = mode;
        }
    }

    private void UpdateGameModeButtons()
    {
        if (GameData.gameModes == null || GameData.gameModes.Count == 0)
        {
            return;
        }
        int activeBtnsCount = 0;
        for (int i = 0; i < modeButtonGroup.ToggleBtns.Length; i++)
        {
            GameData.GameMode btnGameMode = BtnIndexToGameMode(i);
            modeButtonGroup.ToggleBtns[i].gameObject.SetActive(GameData.gameModes.Contains(btnGameMode));
            if (GameData.gameModes.Contains(BtnIndexToGameMode(i)))
            {
                activeBtnsCount++;
            }
        }
        //Если доступен только один режим - выключаем кнопки выбора режимов
        modeButtonGroup.gameObject.SetActive(activeBtnsCount > 1);
    }

    private GameData.GameMode BtnIndexToGameMode(int btnIndex)
    {
        return (GameData.GameMode)(btnIndex + 1);
    }

    /// <summary>
    /// If argument is random map - find random map, else return argument
    /// </summary>
    public static GameManager.MapId GetMapForBattle(GameManager.MapId _mapId)
    {
        if (_mapId != GameManager.MapId.random_map)
        {
            Debug.LogFormat("Go to map {0}.", _mapId);
            return _mapId;
        }
        else
        {
            Debug.LogFormat("Go to map {0}, Chosen random map", _mapId);
            return AvailableMaps.GetRandomItem();
        }
    }

}
