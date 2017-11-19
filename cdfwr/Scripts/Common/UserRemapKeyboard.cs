using UnityEngine;
using System.Collections.Generic;
using Rewired;

public class UserRemapKeyboard : HangarPage
{

    [Header("Indexes of actions")]
    public int Fire;
    public int MoveForward;
    public int MoveBackward;
    public int TurnLeft;
    public int TurnRight;
    public int TurretLeft;
    public int TurretRight;
    public int CenterTurret;
    public int Zoom;
    public int Consumable1;
    public int Consumable2;
    public int Consumable3;
    public int CruiseControl;
    public int ForceReload;

    [Header("Buttons For Remap")]
    public tk2dUIItem Fire1;
    public tk2dUIItem Fire2;
    public tk2dUIItem MoveForward1;
    public tk2dUIItem MoveForward2;
    public tk2dUIItem MoveBackward1;
    public tk2dUIItem MoveBackward2;
    public tk2dUIItem TurnLeft1;
    public tk2dUIItem TurnLeft2;
    public tk2dUIItem TurnRight1;
    public tk2dUIItem TurnRight2;
    public tk2dUIItem TurretLeft1;
    public tk2dUIItem TurretLeft2;
    public tk2dUIItem TurretRight1;
    public tk2dUIItem TurretRight2;
    public tk2dUIItem CenterTurret1;
    public tk2dUIItem CenterTurret2;
    public tk2dUIItem Zoom1;
    public tk2dUIItem Zoom2;
    public tk2dUIItem Consumable11;
    public tk2dUIItem Consumable12;
    public tk2dUIItem Consumable21;
    public tk2dUIItem Consumable22;
    public tk2dUIItem Consumable31;
    public tk2dUIItem Consumable32;
    public tk2dUIItem CruiseControlItem1;
    public tk2dUIItem CruiseControlItem2;
    public tk2dUIItem ForceReload1;
    public tk2dUIItem ForceReload2;

    [Header("NoneKey")]
    public string NoneKey = "Не назначено";

    private const int ITEMS_CAPACITY = 28;
    private const float ASSIGMENT_TIMEOUT = 5.0f;

    private tk2dUIItem[] items;
    private tk2dUIItem itemOnRemapKey;

    private Player player;
    private ControllerMap selectedMap;
    private ElementAssignmentChange currentAssignmentChange;

    private bool initialized;
    private bool remapInProcess;
    private bool readyToPool;

    private float time;
    private int seconds;

    // Меняем этот ключ, если хотим чтобы у игроков сбросили настройки rewired, например при добавлении новых кнопок rewired в билд.
    public const string PlayerPrefsBaseKey = "CodeOfWar";
    public const string RewiredRefreshKey = "RewiredSetted23082017";

    void OnEnable()
    {
        items = new tk2dUIItem[ITEMS_CAPACITY] { Fire1, Fire2, MoveForward1, MoveForward2, MoveBackward1, MoveBackward2,
        TurnLeft1, TurnLeft2, TurnRight1, TurnRight2,TurretLeft1,TurretLeft2,TurretRight1,TurretRight2,CenterTurret1,CenterTurret2,Zoom1,Zoom2,Consumable11,
        Consumable12,Consumable21,Consumable22,Consumable31,Consumable32,CruiseControlItem1,CruiseControlItem2,ForceReload1,ForceReload2};
        player = ReInput.players.GetPlayers(true)[1];
        if (PlayerPrefs.HasKey(RewiredRefreshKey))
        {
            LoadAllMaps();
        }
        else
        {
            ResetOnDefault();
            SaveAllMaps();
            PlayerPrefs.SetInt(RewiredRefreshKey, 1);
        }
        Initialize();
    }

    private void Initialize()
    {
        selectedMap = null;
        selectedMap = player.controllers.maps.GetFirstMapInCategory(0, 1, 1);
        initialized = true;
        LIbutton(Fire1.name, Fire2.name, Fire);
        LIbutton(Zoom1.name, Zoom2.name, Zoom);
        LIbutton(CenterTurret1.name, CenterTurret2.name, CenterTurret);
        LIbutton(Consumable11.name, Consumable12.name, Consumable1);
        LIbutton(Consumable21.name, Consumable22.name, Consumable2);
        LIbutton(Consumable31.name, Consumable32.name, Consumable3);
        LIbutton(CruiseControlItem1.name, CruiseControlItem2.name, CruiseControl);
        LIbutton(ForceReload1.name, ForceReload2.name, ForceReload);
        LIAxis(TurnRight1.name, TurnRight2.name, TurnLeft1.name, TurnLeft2.name, TurnRight);
        LIAxis(TurretRight1.name, TurretRight2.name, TurretLeft1.name, TurretLeft2.name, TurretRight);
        LIAxis(MoveForward1.name, MoveForward2.name, MoveBackward1.name, MoveBackward2.name, MoveForward);
    }

    public void LateUpdate()
    {
        if (!initialized) return;
        if (!readyToPool) return;
        Timer();
        KeyPool();
    }

    private void Timer()
    {
        time += Time.deltaTime;
        seconds = (int)(time % 60);
        if (seconds % 1 == 0)
        {
            if (seconds >= ASSIGMENT_TIMEOUT)
            {
                readyToPool = false;
                AfterRemapOrTimeout();
            }
        }
    }

    #region Buttons

    public void ClickButton(tk2dUIItem uiItem)
    {
        if (remapInProcess) return;
        remapInProcess = true;
        readyToPool = true;
        TweenBase tweenScript = uiItem.GetComponentInParent<TweenBlink>();
        if (tweenScript)
        {
            tweenScript.SetActiveAnimation(true);
        }
        foreach (InputAction action in ReInput.mapping.ActionsInCategory(0))
        {
            if (action.id == ForceReload)
            {
                Click2StageButton(uiItem, action, ForceReload1.name, ForceReload2.name);
            }
            if (action.id == CruiseControl)
            {
                Click2StageButton(uiItem, action, CruiseControlItem1.name, CruiseControlItem2.name);
            }
            if (action.id == Consumable1)
            {
                Click2StageButton(uiItem, action, Consumable11.name, Consumable12.name);
            }
            if (action.id == Consumable2)
            {
                Click2StageButton(uiItem, action, Consumable21.name, Consumable22.name);
            }
            if (action.id == Consumable3)
            {
                Click2StageButton(uiItem, action, Consumable31.name, Consumable32.name);
            }
            if (action.id == Fire)
            {
                Click2StageButton(uiItem, action, Fire1.name, Fire2.name);
            }
            if (action.id == Zoom)
            {
                Click2StageButton(uiItem, action, Zoom1.name, Zoom2.name);
            }
            if (action.id == CenterTurret)
            {
                Click2StageButton(uiItem, action, CenterTurret1.name, CenterTurret2.name);
            }
            if (action.id == MoveForward || action.id == MoveBackward)
            {
                Click2StageAxis(uiItem, action, MoveForward1.name, MoveForward2.name, MoveBackward1.name, MoveBackward2.name);
            }
            if (action.id == TurnRight || action.id == TurnLeft)
            {
                Click2StageAxis(uiItem, action, TurnRight1.name, TurnRight2.name, TurnLeft1.name, TurnLeft2.name);
            }
            if (action.id == TurretRight || action.id == TurretLeft)
            {
                Click2StageAxis(uiItem, action, TurretRight1.name, TurretRight2.name, TurretLeft1.name, TurretLeft2.name);
            }
        }
    }

    private void LIAxis(string posFirstName, string posSecondName, string negFirstName, string negSecondName, int actionId)
    {
        IList<ActionElementMap> positiveElements = new List<ActionElementMap>();
        IList<ActionElementMap> negativeElements = new List<ActionElementMap>();
        foreach (tk2dUIItem item in items)
        {
            var tMesh = item.GetComponent<tk2dTextMesh>();
            foreach (InputAction action in ReInput.mapping.ActionsInCategory(0))
            {
                if (action.id == actionId)
                {
                    foreach (ActionElementMap elementMap in selectedMap.AllMaps)
                    {
                        if (elementMap.actionId == action.id && elementMap.axisContribution == Pole.Positive)
                        {
                            positiveElements.Add(elementMap);
                        }
                        else if (elementMap.actionId == action.id && elementMap.axisContribution == Pole.Negative)
                        {
                            negativeElements.Add(elementMap);
                        }
                    }
                    foreach (ActionElementMap elementMap in positiveElements)
                    {
                        if (elementMap.actionId != action.id) continue;

                        if (elementMap == positiveElements[0] && (item.name.Equals(posFirstName)))
                        {
                            tMesh.text = elementMap.keyCode.ToString();
                        }
                        else if (elementMap == positiveElements[1] && (item.name.Equals(posSecondName)))
                        {
                            tMesh.text = elementMap.keyCode.ToString();
                        }
                    }
                    foreach (ActionElementMap elementMap in negativeElements)
                    {
                        if (elementMap == negativeElements[0] && (item.name.Equals(negFirstName)))
                        {
                            tMesh.text = elementMap.keyCode.ToString();
                        }
                        else if (elementMap == negativeElements[1] && (item.name.Equals(negSecondName)))
                        {
                            tMesh.text = elementMap.keyCode.ToString();
                        }
                        if (tMesh.text == KeyCode.None.ToString())
                        {
                            tMesh.text = NoneKey;
                        }
                    }
                }
            }
        }
    }

    private void LIbutton(string firstName, string secondName, int actionId)
    {
        IList<ActionElementMap> validElements = new List<ActionElementMap>();
        foreach (tk2dUIItem item in items)
        {
            var tMesh = item.GetComponent<tk2dTextMesh>();
            foreach (InputAction action in ReInput.mapping.ActionsInCategory(0))
            {
                if (action.id == actionId)
                {
                    if (action.type == InputActionType.Button)
                    {
                        foreach (ActionElementMap elementMap in selectedMap.AllMaps)
                        {
                            if (elementMap.actionId != action.id) continue;
                            validElements.Add(elementMap);
                        }
                        foreach (ActionElementMap elementMap in selectedMap.AllMaps)
                        {
                            if (elementMap.actionId != action.id) continue;
                            if (elementMap == validElements[0] && (item.name.Equals(firstName)))
                            {
                                tMesh.text = elementMap.keyCode.ToString();
                            }
                            else if (elementMap == validElements[1] && (item.name.Equals(secondName)))
                            {
                                tMesh.text = elementMap.keyCode.ToString();
                            }
                            if (tMesh.text == KeyCode.None.ToString())
                            {
                                tMesh.text = NoneKey;
                            }
                        }
                    }
                }
            }
        }
    }

    private void Click2StageButton(tk2dUIItem item, InputAction action, string firstButton, string secondButton)
    {
        IList<ActionElementMap> validElements = new List<ActionElementMap>();
        foreach (ActionElementMap elementMap in selectedMap.AllMaps)
        {
            if (elementMap.actionId != action.id) continue;
            validElements.Add(elementMap);
        }
        foreach (ActionElementMap elementMap in selectedMap.AllMaps)
        {
            if (elementMap.actionId != action.id) continue;
            if (elementMap == validElements[0] && (item.name.Equals(firstButton)))
            {
                itemOnRemapKey = item;
                currentAssignmentChange = new ElementAssignmentChange(elementMap.id, action.id, Pole.Positive);
            }
            else if (elementMap == validElements[1] && (item.name.Equals(secondButton)))
            {
                itemOnRemapKey = item;
                currentAssignmentChange = new ElementAssignmentChange(elementMap.id, action.id, Pole.Positive);
            }
        }
    }

    private void Click2StageAxis(tk2dUIItem item, InputAction action, string pos1, string pos2,
        string neg1, string neg2)
    {
        IList<ActionElementMap> positiveElements = new List<ActionElementMap>();
        IList<ActionElementMap> negativeElements = new List<ActionElementMap>();
        foreach (ActionElementMap elementMap in selectedMap.AllMaps)
        {
            if (elementMap.actionId == action.id && elementMap.axisContribution == Pole.Positive)
            {
                positiveElements.Add(elementMap);
            }
            else if (elementMap.actionId == action.id && elementMap.axisContribution == Pole.Negative)
            {
                negativeElements.Add(elementMap);
            }
        }
        foreach (ActionElementMap elementMap in positiveElements)
        {
            if (elementMap.actionId != action.id) continue;

            if (elementMap == positiveElements[0] && (item.name.Equals(pos1)))
            {
                itemOnRemapKey = item;
                currentAssignmentChange = new ElementAssignmentChange(elementMap.id, action.id, Pole.Positive);
            }
            else if (elementMap == positiveElements[1] && (item.name.Equals(pos2)))
            {
                itemOnRemapKey = item;
                currentAssignmentChange = new ElementAssignmentChange(elementMap.id, action.id, Pole.Positive);
            }
        }
        foreach (ActionElementMap elementMap in negativeElements)
        {
            if (elementMap == negativeElements[0] && (item.name.Equals(neg1)))
            {
                itemOnRemapKey = item;
                currentAssignmentChange = new ElementAssignmentChange(elementMap.id, action.id, Pole.Negative);
            }
            else if (elementMap == negativeElements[1] && (item.name.Equals(neg2)))
            {
                itemOnRemapKey = item;
                currentAssignmentChange = new ElementAssignmentChange(elementMap.id, action.id, Pole.Negative);
            }
        }
    }

    #endregion

    #region PoolAndRemap

    private void KeyPool()
    {
        foreach (ControllerPollingInfo info in ReInput.controllers.Keyboard.PollForAllKeys())
        {
            if (info.keyboardKey != KeyCode.None)
            {
                readyToPool = false;
                CheckSameKey(currentAssignmentChange, info.keyboardKey);
            }
        }
    }

    private void CheckSameKey(ElementAssignmentChange entry, KeyCode key)
    {
        if (entry == null || key == KeyCode.None) return;
        foreach (tk2dUIItem item in items)
        {
            if (item.GetComponent<tk2dTextMesh>().text.Equals(key.ToString()))
            {
                foreach (var elementMap in selectedMap.AllMaps)
                {
                    if (elementMap.keyCode == key && !Equals(KeyCode.None))
                    {
                        selectedMap.ReplaceElementMap(elementMap.id, elementMap.actionId,
                            elementMap.axisContribution, KeyCode.None, ModifierKey.None, ModifierKey.None,
                            ModifierKey.None);
                        item.GetComponent<tk2dTextMesh>().text = NoneKey;
                    }
                }
            }
        }
        Remap(entry, key);
    }

    private void Remap(ElementAssignmentChange entry, KeyCode key)
    {
        selectedMap.ReplaceElementMap(entry.ActionElementMapId, entry.ActionId, entry.ActionAxisContribution,
             key, ModifierKey.None, ModifierKey.None, ModifierKey.None);
        AfterRemapOrTimeout();
    }

    private void AfterRemapOrTimeout()
    {
        var item = itemOnRemapKey.GetComponentInParent<TweenBlink>();
        seconds = 0;
        time = 0;
        item.SetActiveAnimation(false);
        item.stateOnAwake = true;
        currentAssignmentChange = null;
        remapInProcess = false;
        Initialize();
    }
    #endregion

    #region Load/Save

    public void LoadAllMaps()
    {
        IList<InputBehavior> behaviors = ReInput.mapping.GetInputBehaviors(player.id);
        for (int j = 0; j < behaviors.Count; j++)
        {
            string xml = GetInputBehaviorXml(player, behaviors[j].id);
            if (xml == null || xml == string.Empty) continue;
            behaviors[j].ImportXmlString(xml);
        }
        List<string> keyboardMaps = GetAllControllerMapsXml(player, true, Rewired.ControllerType.Keyboard, ReInput.controllers.Keyboard);
        if (keyboardMaps.Count > 0)
        {
            player.controllers.maps.ClearMaps(Rewired.ControllerType.Keyboard, true);
        }
        player.controllers.maps.AddMapsFromXml(Rewired.ControllerType.Keyboard, 0, keyboardMaps);
        Initialize();
    }

    public void ResetOnDefault()
    {
        player.controllers.maps.LoadDefaultMaps(Rewired.ControllerType.Keyboard);
        Initialize();
    }

    public void SaveAllMaps()
    {
        PlayerSaveData playerData = player.GetSaveData(true);

        foreach (InputBehavior behavior in playerData.inputBehaviors)
        {
            string key = GetInputBehaviorPlayerPrefsKey(player, behavior);
            PlayerPrefs.SetString(key, behavior.ToXmlString());
        }

        foreach (ControllerMapSaveData saveData in playerData.AllControllerMapSaveData)
        {
            string key = GetControllerMapPlayerPrefsKey(player, saveData);
            PlayerPrefs.SetString(key, saveData.map.ToXmlString());
        }
        PlayerPrefs.Save();
        GUIPager.Back();
    }
    #region PlayerPrefs Methods

    private string GetBasePlayerPrefsKey(Player player)
    {
        string key = PlayerPrefsBaseKey;
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

    private List<string> GetAllControllerMapsXml(Player player, bool userAssignableMapsOnly, Rewired.ControllerType controllerType, Controller controller)
    {
        List<string> mapsXml = new List<string>();
        IList<InputMapCategory> categories = ReInput.mapping.MapCategories;
        for (int i = 0; i < categories.Count; i++)
        {
            InputMapCategory cat = categories[i];
            if (userAssignableMapsOnly && !cat.userAssignable) continue; // skip map because not user-assignable

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

        if (!PlayerPrefs.HasKey(key)) return string.Empty;
        return PlayerPrefs.GetString(key);
    }

    #endregion

    #endregion

    private class ElementAssignmentChange
    {
        public int ActionElementMapId { get; private set; }
        public int ActionId { get; private set; }
        public Pole ActionAxisContribution { get; private set; }
        public ElementAssignmentChange(
            int actionElementMapId,
            int actionId,
            Pole actionAxisContribution)
        {
            ActionElementMapId = actionElementMapId;
            ActionId = actionId;
            ActionAxisContribution = actionAxisContribution;
        }
    }
}



