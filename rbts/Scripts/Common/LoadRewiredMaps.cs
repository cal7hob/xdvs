using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rewired;

public class LoadRewiredMaps : MonoBehaviour
{ //Скрипт загружает все привязки по управлению из playerPrefs, должен висеть на выборе карт.
    private Player player;
    public string PlayerPrefsBaseKey;

    void Start () {
        player = ReInput.players.GetPlayers(true)[1];
	    LoadAllMaps();
	}

    public void LoadAllMaps()
    {
        IList<InputBehavior> behaviors = ReInput.mapping.GetInputBehaviors(player.id);
        for (int j = 0; j < behaviors.Count; j++)
        {
            string xml = GetInputBehaviorXml(player, behaviors[j].id);
            if (xml == null || xml == string.Empty) continue;
            behaviors[j].ImportXmlString(xml);
        }
        List<string> keyboardMaps = GetAllControllerMapsXml(player, true, ControllerType.Keyboard, ReInput.controllers.Keyboard);
        if (keyboardMaps.Count > 0)
        {
            player.controllers.maps.ClearMaps(ControllerType.Keyboard, true);
        }
        player.controllers.maps.AddMapsFromXml(ControllerType.Keyboard, 0, keyboardMaps);
    }

    private string GetBasePlayerPrefsKey(Player player)
    {
        string key = PlayerPrefsBaseKey;
        key += "|playerName=" + player.name;
        return key;
    }

    private string GetControllerMapXml(Player player, int categoryId, int layoutId, Controller controller)
    {
        string key = GetBasePlayerPrefsKey(player);
        key += "|dataType=ControllerMap";
        key += "|controllerMapType=" + controller.mapTypeString;
        key += "|categoryId=" + categoryId + "|" + "layoutId=" + layoutId;
        key += "|hardwareIdentifier=" + controller.hardwareIdentifier;

        if (!PlayerPrefs.HasKey(key)) return string.Empty;
        return PlayerPrefs.GetString(key);
    }

    private List<string> GetAllControllerMapsXml(Player player, bool userAssignableMapsOnly, ControllerType controllerType, Controller controller)
    {
        List<string> mapsXml = new List<string>();
        IList<InputMapCategory> categories = ReInput.mapping.MapCategories;
        for (int i = 0; i < categories.Count; i++)
        {
            InputMapCategory cat = categories[i];
            if (userAssignableMapsOnly && !cat.userAssignable) continue;

            IList<InputLayout> layouts = ReInput.mapping.MapLayouts(controllerType);
            for (int j = 0; j < layouts.Count; j++)
            {
                InputLayout layout = layouts[j];
                string xml = GetControllerMapXml(player, cat.id, layout.id, controller);
                if (xml == string.Empty) continue;
                mapsXml.Add(xml);
            }
        }
        return mapsXml;
    }
    
    private string GetInputBehaviorXml(Player player, int id)
    {
        string key = GetBasePlayerPrefsKey(player);
        key += "|dataType=InputBehavior";
        key += "|id=" + id;

        if (!PlayerPrefs.HasKey(key)) return string.Empty;
        return PlayerPrefs.GetString(key);
    }
}
