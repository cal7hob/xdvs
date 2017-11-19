using System;
using UnityEngine;
using System.Collections.Generic;
using Rewired;
using System.Linq;

public class UserRemapKeyboard : MonoBehaviour
{
    public enum Category
    {
        Default     = 0,
        Tank        = 1,
        SpaceShip   = 2,
        Aircraft    = 3,
        Helicopter  = 4,
    }

    [SerializeField] private Category category = Category.Tank; 
    [SerializeField] private Factory factory;
    
    private Player player;
    private ControllerMap controllerMap;

    public void Initialize(bool forceRecreateFactoryItems = false)
    {
        //PrepareDataList
        List<int> mapsOrder = GetUniqActionIdList();//remember order of actions

        Dictionary<int, Dictionary<Pole, List<ActionElementMap>>> mapsDict = GetAllMapsDic();

        List<KeyRemapItem.Data> data = new List<KeyRemapItem.Data>();
        for (int i = 0; i < mapsOrder.Count; i++)
            foreach(Pole pole in Enum.GetValues(typeof(Pole)))
                if (mapsDict[mapsOrder[i]] != null && mapsDict[mapsOrder[i]].ContainsKey(pole))
                {
                    data.Add(new KeyRemapItem.Data(
                        i,
                        GetInputActionById(mapsOrder[i]),
                        pole,
                        mapsDict[mapsOrder[i]][pole]));
                }

        //Instantiate or update items
        factory.CreateAll(data, false, new ParamDict().Add("parentPanel", this));
    }

    private List<int> GetUniqActionIdList()
    {
        List<int> mapsOrder = new List<int>();
        foreach (ActionElementMap map in controllerMap.AllMaps)
            if (!mapsOrder.Contains(map.actionId))
                mapsOrder.Add(map.actionId);
        return mapsOrder;
    }

    private Dictionary<int, Dictionary<Pole, List<ActionElementMap>>> GetAllMapsDic()
    {
        Dictionary<int, Dictionary<Pole, List<ActionElementMap>>> mapsDict = new Dictionary<int, Dictionary<Pole, List<ActionElementMap>>>();
        foreach (ActionElementMap map in controllerMap.AllMaps)
        {
            if (!mapsDict.ContainsKey(map.actionId))
                mapsDict[map.actionId] = new Dictionary<Pole, List<ActionElementMap>>();
            if (!mapsDict[map.actionId].ContainsKey(map.axisContribution))
                mapsDict[map.actionId][map.axisContribution] = new List<ActionElementMap>();
            mapsDict[map.actionId][map.axisContribution].Add(map);
        }
        return mapsDict;
    }

    private InputAction GetInputActionById(int id)
    {
        foreach (InputAction action in ReInput.mapping.ActionsInCategory(0))
            if (action.id == id)
                return action;
        return null;
    }

    private void OnEnable()
    {
        Initialize();
    }

    public void Remap(KeyRemapItem.Data data, KeyRemapItem.Data.ButtonType btnType, KeyCode key)
    {
        ActionElementMap actionElementMap = data.GetButtonActionElementMap(btnType);

        //Clear conflicted key if exists
        foreach (var elementMap in controllerMap.AllMaps)
            if (elementMap.keyCode == key && key != KeyCode.None && actionElementMap != elementMap)
                controllerMap.ReplaceElementMap(elementMap.id, elementMap.actionId, elementMap.axisContribution, KeyCode.None, ModifierKey.None, ModifierKey.None, ModifierKey.None);

        if (actionElementMap != null)//если такой элемент есть - заменяем
            controllerMap.ReplaceElementMap(actionElementMap.id, data.InputAction.id, data.AxisContribution, key, ModifierKey.None, ModifierKey.None, ModifierKey.None);
        else//иначе - создаем
            controllerMap.CreateElementMap(data.InputAction.id, data.AxisContribution, key, ModifierKey.None, ModifierKey.None, ModifierKey.None);

        Initialize();
    }

    private void OnClick(tk2dUIItem btn)
    {
        switch(btn.name)
        {
            case "btnCancel":
                LoadAllMaps();
                GUIPager.Back();
                break;
            case "btnReset":
                MessageBox.Show(MessageBox.Type.Question, Localizer.GetText("SettingsResetConfirmation"),
                    (MessageBox.Answer answer) =>
                    {
                        if (answer == MessageBox.Answer.Yes)
                        {
                            ResetOnDefault();
                            Initialize(true);//recreate items to prevent count difference
                        }
                    });
                break;
            case "btnSave":
                SaveAllMaps();
                GUIPager.Back();
                break;
        }
    }

    public bool IsAnyItemRemapInProcess()
    {
        for(int i = 0; i < factory.Items.Count; i++)
        {
            KeyRemapItem item = (KeyRemapItem)factory.Items[i];
            if (item.IsRemapInProcess)
                return true;
        }
        return false;
    }

    #region Load/Save
    public void LoadAllMaps()
    {
        player = ReInput.players.GetPlayers(true)[1];
        IList<InputBehavior> behaviors = ReInput.mapping.GetInputBehaviors(player.id);
        for (int j = 0; j < behaviors.Count; j++)
        {
            string xml = GetInputBehaviorXml(player, behaviors[j].id);
            if (string.IsNullOrEmpty(xml))
                continue;
            behaviors[j].ImportXmlString(xml);
        }

        List<string> keyboardMaps = GetAllControllerMapsXml(player, true, ControllerType.Keyboard, ReInput.controllers.Keyboard);//saved maps, can be different with actual maps

        if (keyboardMaps.Count > 0)
        {
            //current build maps
            controllerMap = player.controllers.maps.GetFirstMapInCategory(0, 1, (int)category);
            List<int> currentActionIdList = GetUniqActionIdList();

            //maps from registry
            player.controllers.maps.ClearMaps(ControllerType.Keyboard, true);
            player.controllers.maps.AddMapsFromXml(ControllerType.Keyboard, 0, keyboardMaps);
            controllerMap = player.controllers.maps.GetFirstMapInCategory(0, 1, (int)category);
            XDevs.Input.UpdateMapsStatus();
            List<int> registryActionIdList = GetUniqActionIdList();
            try
            {
                if (AreActionListsDifferent(currentActionIdList, registryActionIdList))//for example, action was added/deleted in new build
                {
                    Debug.LogWarningFormat("Rewired Actions lists are different!\ncurrent: {0}\nregistry: {1}", currentActionIdList.ToJsonString(), registryActionIdList.ToJsonString());
                    Dictionary<int, Dictionary<Pole, List<ActionElementMap>>> oldDict = GetAllMapsDic();
                    ResetOnDefault();
                    Dictionary<int, Dictionary<Pole, List<ActionElementMap>>> newDic = GetAllMapsDic();

                    #region Megre old keyCodes with new
                    foreach (var actionIdPair in newDic)
                    {
                        foreach (var polePair in actionIdPair.Value)
                        {
                            if (oldDict.ContainsKey(actionIdPair.Key) && oldDict[actionIdPair.Key].ContainsKey(polePair.Key))
                            {
                                for (int i = 0; i < oldDict[actionIdPair.Key][polePair.Key].Count; i++)
                                {
                                    if (polePair.Value.Count > i)
                                    {
                                        if (polePair.Value[i].keyCode != oldDict[actionIdPair.Key][polePair.Key][i].keyCode)
                                        {
                                            Debug.LogWarningFormat("ReplaceElementMap actionId {0}, id = {1}, pole = {2}, btnIndex = {3}, new keyCode = {4}", actionIdPair.Key, polePair.Value[i].id, polePair.Key, i, oldDict[actionIdPair.Key][polePair.Key][i].keyCode);
                                            controllerMap.ReplaceElementMap(polePair.Value[i].id, actionIdPair.Key, polePair.Key, oldDict[actionIdPair.Key][polePair.Key][i].keyCode, ModifierKey.None, ModifierKey.None, ModifierKey.None);
                                        }
                                    }
                                    else
                                    {
                                        Debug.LogWarningFormat("CreateElementMap actionId {0}, pole = {1}, btnIndex = {2}, new keyCode = {3}", actionIdPair.Key, polePair.Key, i, oldDict[actionIdPair.Key][polePair.Key][i].keyCode);
                                        controllerMap.CreateElementMap(actionIdPair.Key, polePair.Key, oldDict[actionIdPair.Key][polePair.Key][i].keyCode, ModifierKey.None, ModifierKey.None, ModifierKey.None);
                                    }
                                }
                            }
                        }
                    }
                    #endregion

                    SaveAllMaps();
                }
            }
            catch(Exception ex)
            {
                Debug.LogErrorFormat("Rewired migration Error. Text: {0}", ex.Message);
                ResetOnDefault();
                return;
            }
        }
        else
        {
            ResetOnDefault();//empty registry, like first enter the game
        }
    }

    /// <summary>
    /// Compare 2 lists by content
    /// true if lists contain different items, order ignored
    /// </summary>
    private bool AreActionListsDifferent(List<int> list1, List<int> list2)
    {
        if (list1 == null || list2 == null || list1.Count != list2.Count)
            return true;

        HashSet<int> hashSet = new HashSet<int>();
        for (int i = 0; i < list1.Count; i++)
            hashSet.Add(list1[i]);

        for(int i = 0; i < list2.Count; i++)
        {
            if (!hashSet.Contains(list2[i]))
                return true;
            else
                hashSet.Remove(list2[i]);
        }

        return hashSet.Count > 0;
    }

    private void SaveAllMaps()
    {
        PlayerSaveData playerData = player.GetSaveData(true);

        foreach (InputBehavior behavior in playerData.inputBehaviors)
            PlayerPrefs.SetString(GetInputBehaviorPlayerPrefsKey(player, behavior), behavior.ToXmlString());

        foreach (ControllerMapSaveData saveData in playerData.AllControllerMapSaveData)
            PlayerPrefs.SetString(GetControllerMapPlayerPrefsKey(player, saveData), saveData.map.ToXmlString());

        PlayerPrefs.Save();
    }

    private void ResetOnDefault()
    {
        player.controllers.maps.LoadDefaultMaps(ControllerType.Keyboard);
        controllerMap = player.controllers.maps.GetFirstMapInCategory(0, 1, (int)category);//keyboard, 
        SaveAllMaps();
    }
    #endregion

    #region PlayerPrefs Methods

    private string GetBasePlayerPrefsKey(Player player)
    {
        string key = GameData.CurInterface.ToString();
        key += "|playerName=" + player.name;
        return key;
    }

    private string GetControllerMapPlayerPrefsKey(Player player, ControllerMapSaveData saveData)
    {
        string key = GetBasePlayerPrefsKey(player);
        key += "|dataType=ControllerMap";
        key += "|controllerMapType=" + saveData.mapTypeString;
        key += "|categoryId=" + saveData.map.categoryId + "|" + "layoutId=" + saveData.map.layoutId;
        key += "|hardwareIdentifier=" + saveData.controllerHardwareIdentifier;
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
            if (userAssignableMapsOnly && !categories[i].userAssignable)
                continue; // skip map because not user-assignable

            IList<InputLayout> layouts = ReInput.mapping.MapLayouts(controllerType);
            for (int j = 0; j < layouts.Count; j++)
            {
                InputLayout layout = layouts[j];
                string xml = GetControllerMapXml(player, categories[i].id, layout.id, controller);
                if (string.IsNullOrEmpty(xml))
                    continue;
                mapsXml.Add(xml);
            }
        }
        return mapsXml;
    }

    private string GetInputBehaviorPlayerPrefsKey(Player player, InputBehavior saveData)
    {
        string key = GetBasePlayerPrefsKey(player);
        key += "|dataType=InputBehavior";
        key += "|id=" + saveData.id;
        return key;
    }

    private string GetInputBehaviorXml(Player player, int id)
    {
        string key = GetBasePlayerPrefsKey(player);
        key += "|dataType=InputBehavior";
        key += "|id=" + id;

        return !PlayerPrefs.HasKey(key) ? string.Empty : PlayerPrefs.GetString(key);
    }

    #endregion
}