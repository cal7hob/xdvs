using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
////Включение объектов для текущего режима игры, выключние всех остальных
/// </summary>
public class GameModeObjectsSwitcher: MonoBehaviour
{
    /// <summary>
    ////Пришлось сделать такой класс потому что юнити не поддерживает сериализацию словарей
    /// </summary>
    [Serializable]
    public class DummyClass
    {
        public GameData.GameMode gameMode;
        public List<GameObject> list;
    }

    public List<DummyClass> objects;

    private void Awake ()
    {
        Messenger.Subscribe(EventId.GameModeChanged, OnGameModeChanged);
    }

    private void Start()
    {
        UpdateObjects();
    }

    private void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.GameModeChanged, OnGameModeChanged);
    }

    private void OnGameModeChanged(EventId id, EventInfo ei)
    {
        UpdateObjects();
    }

    private void UpdateObjects()
    {
        if (objects != null)
            for (int i = 0; i < objects.Count; i++)
                if (objects[i] != null)
                    MiscTools.SetObjectsActivity(objects[i].list, GameData.Mode == objects[i].gameMode);
    }

}
