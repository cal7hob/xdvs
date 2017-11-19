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
        Dispatcher.Subscribe(EventId.GameModeChanged, UpdateObjects);
    }

    private void Start()
    {
        UpdateObjects();
    }

    private void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.GameModeChanged, UpdateObjects);
    }

    public void UpdateObjects(EventId id = EventId.Manual, EventInfo ei = null)
    {
        if (objects != null)
            for (int i = 0; i < objects.Count; i++)
                if (objects[i] != null)
                    MiscTools.SetObjectsActivity(objects[i].list, GameData.Mode == objects[i].gameMode);
    }

}
