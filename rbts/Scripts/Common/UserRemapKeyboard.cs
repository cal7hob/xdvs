using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Rewired;

public class UserRemapKeyboard : MonoBehaviour
{
    #region Actions>ActionKeys>KeyMap(KeyUIItem+ActionElementMap+changeKeyCode)
    [System.Serializable]
    public class Actions : AEDictionary<int, ActionKeys>
    {
        //[System.Serializable]
        private class Dictionary2Key : AEDictionary<tk2dUIItem, KeyMap>
        {
            private List<KeyCode> keys2 = new List<KeyCode>();

            public override void Add(tk2dUIItem key, KeyMap value)
            {
                if (keys2.Contains(value.map.keyCode)) return;
                keys2.Add(value.map.keyCode);
                keys.Add(key);
                values.Add(value);
            }

            public KeyMap Get(KeyCode key)
            {
                int index = keys2.IndexOf(key);
                if (index == -1) return null;
                return values[index];
            }

            public void ClearKeyCode(KeyCode key) //KeyMap
            {
                int index = keys2.IndexOf(key);
                if (index == -1) return;
                keys2[index] = KeyCode.None;
                //return values[index];
            }

            public void UpdateKeyCode(KeyMap keyMap)
            {
                int index = values.IndexOf(keyMap);
                if (index == -1) return;
                keys2[index] = keyMap.changeKeyCode;
            }
        }

        private Dictionary2Key uIItemToKeyMap = new Dictionary2Key();
        private KeyMap select;
        public List<KeyMap> changeKeyMaps = new List<KeyMap>();

        public void Init(IList<ActionElementMap> actionElementMapsDefault)
        {
            ControllerMap controllerMap = ReInput.players.GetPlayers(true)[1].controllers.maps.GetFirstMapInCategory(0, 1, 1);
            if (controllerMap.ButtonMaps.Count != actionElementMapsDefault.Count) //fix add new default maps in save
            {
                List<int> actionsSaveMaps = new List<int>();
                int key;
                foreach (ActionElementMap actionElementMap in controllerMap.ButtonMaps)
                {
                    if (!actionsSaveMaps.Contains(key = GetKey(actionElementMap))) actionsSaveMaps.Add(key);
                }

                if (controllerMap.ButtonMaps.Count > actionElementMapsDefault.Count) //fix remove old maps in save
                {
                    actionsSaveMaps.Clear();
                    foreach (ActionElementMap actionElementMap in actionElementMapsDefault)
                    {
                        if (!actionsSaveMaps.Contains(key = GetKey(actionElementMap))) actionsSaveMaps.Add(key);
                    }
                }
            }
            Init();
        }

        public void Init()
        {
            if (instance.scrollableArea == null)
            {
                Debug.LogError("UserRemapKeyboard: Not configured: scrollableArea");
                return;
            }

            if (instance.keyboardItemPrefab == null)
            {
                Debug.LogError("UserRemapKeyboard: Not configured: keyboardItemPrefab");
                return;
            }

            foreach (ActionElementMap actionElementMap in ReInput.players.GetPlayers(true)[1].controllers.maps.GetFirstMapInCategory(0, 1, 1).AllMaps) Add(actionElementMap); // Get actions
            //instance.scrollableArea.ContentLength = Count * 75 - instance.scrollableArea.VisibleAreaLength;

            int itemPositionY = -83;
            KeyMap keyMap;
            int count = 0;
            foreach (ActionKeys actionKey in values) //Instantiate keyboardItem
            {
                if (actionKey.enabled)
                {
                    foreach (KeyUIItem keyItem in KeyboardUIItem.Create(actionKey.name, itemPositionY).keys)
                    {//Add keyMaps and add UIItem link to keyMaps
                        if ((keyMap = actionKey.Add(keyItem)) != null)
                        {
                            uIItemToKeyMap.Add(keyItem.uIItem, keyMap);
                        }
                    }
                    count++;
                    itemPositionY -= 75;
                }
                else
                {
                    foreach (KeyMap keyMap_ in actionKey.keyMaps)
                    {
                        uIItemToKeyMap.Add(null, keyMap_);
                    }
                    
                }
            }
            instance.scrollableArea.ContentLength = count * 75 - instance.scrollableArea.VisibleAreaLength;
        }

        private ActionKeys Get(ActionElementMap actionElementMap)
        {
            return this[GetKey(actionElementMap)];
        }

        private void Add(ActionElementMap actionElementMap)
        {
            //if (ActionKeys.GetName(actionElementMap).Contains("[Hide]")) return; //NotUseCustomKey //temp fix, removed settings consumable with missing localization keys, see the comment in more detail http://redmine.scifi-tanks.com/issues/8485

            int key = GetKey(actionElementMap);
            int index = keys.IndexOf(key);
            if (index == -1)
            {
                keys.Add(key);
                values.Add(new ActionKeys(actionElementMap));
            }
            else
            {
                values[keys.IndexOf(key)].Add(actionElementMap);
            }
        }

        
        private int GetKey(ActionElementMap actionElementMap)
        {
            return actionElementMap.axisContribution == Pole.Positive ? actionElementMap.actionId + 1 : -actionElementMap.actionId - 1;
        }

        public void Select(tk2dUIItem uiItem)
        {
            select = uIItemToKeyMap[uiItem];
            if (select != null)
            {
                select.SetActive(true);
            }
        }

        public void Deselect()
        {
            if (select != null) select.SetActive(false);
        }

        public void ChangeKey(KeyCode key)
        {
            //KeyMap clearKeyMap = uIItemToKeyMap.ClearKeyCode(key); //CheckKey
            KeyMap clearKeyMap = uIItemToKeyMap.Get(key);
            if (clearKeyMap != null)
            {
                if (!Get(clearKeyMap.map).enabled)
                {
                    Deselect();
                    return;
                }
                uIItemToKeyMap.ClearKeyCode(key);
                clearKeyMap.SetKey(KeyCode.None); //CheckKey
                if (!changeKeyMaps.Contains(clearKeyMap)) changeKeyMaps.Add(clearKeyMap);
            }

            select.SetKey(key);
            if (!changeKeyMaps.Contains(select)) changeKeyMaps.Add(select);
            uIItemToKeyMap.UpdateKeyCode(select);
            
            Deselect();
        }

        public void SendChanges()
        {
            foreach (KeyMap keyMap in changeKeyMaps)
            {
                keyMap.SendChangeInRewired();
            }
            changeKeyMaps.Clear();
            Deselect();
        }

        public void DiscardChange()
        {
            if (changeKeyMaps.Count == 0) return;
            foreach (KeyMap keyMap in changeKeyMaps)
            {
                keyMap.Update();
                uIItemToKeyMap.UpdateKeyCode(keyMap);
            }
            changeKeyMaps.Clear();
        }

        public void Update()
        {
            foreach (ActionKeys actionKey in values) actionKey.ReInit();
            ActionKeys actionKeys;
            KeyMap keyMap;
            foreach (ActionElementMap actionElementMap in ReInput.players.GetPlayers(true)[1].controllers.maps.GetFirstMapInCategory(0, 1, 1).AllMaps)
            {
                if ((actionKeys = Get(actionElementMap)) != null)
                {
                    if ((keyMap = actionKeys.ReplaceMap(actionElementMap)) != null)
                    {
                        if (keyMap.Update()) uIItemToKeyMap.UpdateKeyCode(keyMap);
                    }
                }
            }
        }
    }

    [System.Serializable]
    public class ActionKeys
    {
        public ActionKeys(ActionElementMap actionElementMap)
        {
            indexColumn = 0;
            name = GetName(actionElementMap);
            if (name.Contains("[Hide]")) enabled = false;

            //fix name for key localize
            name = name.Replace(" ", "");
            //if(name.Contains("Turret")) name = name.Replace("Turn", "");
            Add(actionElementMap);
        }

        private int indexColumn = 0;
        private const int maxCount = 5;
        public string name = "";
        public List<KeyMap> keyMaps = new List<KeyMap>();
        public bool enabled = true;

        public static string GetName(ActionElementMap actionElementMap)
        {
            InputAction inputAction = ReInput.mapping.GetAction(actionElementMap.actionId);
            return inputAction.type == InputActionType.Button ? inputAction.descriptiveName : actionElementMap.axisContribution == Pole.Positive ? inputAction.positiveDescriptiveName : inputAction.negativeDescriptiveName;

        }

        public void ReInit()
        {
            indexColumn = 0;
        }

        public KeyMap ReplaceMap(ActionElementMap actionElementMap)
        {
            if (indexColumn < keyMaps.Count)
            {
                KeyMap keyMap = keyMaps[indexColumn];
                keyMap.map = actionElementMap;
                indexColumn++;
                return keyMap;
            }
            return null;
        }

        public void Add(ActionElementMap actionElementMap)
        {
            if (keyMaps.Count < maxCount)
            {
                keyMaps.Add(new KeyMap(actionElementMap));
            }
        }

        public KeyMap Add(KeyUIItem keyUIItem)
        {
            if (indexColumn < keyMaps.Count)
            {
                KeyMap keyMap = keyMaps[indexColumn];
                keyMap.keyUIItem = keyUIItem;
                keyUIItem.textMesh.text = keyMap.map.keyCode.ToString();
                indexColumn++;
                return keyMap;
            }
            return null;
        }
    }

    [System.Serializable]
    public class KeyMap
    {
        public KeyMap(ActionElementMap actionElementMap)
        {
            map = actionElementMap;
            changeKeyCode = actionElementMap.keyCode;
        }

        public KeyUIItem keyUIItem;
        public ActionElementMap map;
        public KeyCode changeKeyCode;

        public void SetActive(bool active)
        {
            if (active)
            {
                keyUIItem.textMesh.text = "_";
                if (keyUIItem.activator != null) keyUIItem.activator.color = instance.inRemapFrameColor; //Get(keyMap.map).
                keyUIItem.background.color = instance.inRemapColorBackground;
            }
            else
            {
                keyUIItem.textMesh.text = changeKeyCode.ToString();
                if(keyUIItem.activator != null) keyUIItem.activator.color = instance.baseFrameColor; //Get(keyMap.map).
                keyUIItem.background.color = instance.baseColorBackground;
                keyUIItem.animator.stateOnAwake = true;
            }
            keyUIItem.animator.SetActiveAnimation(active);
        }

        public void SetKey(KeyCode key)
        {
            keyUIItem.textMesh.text = key.ToString();
            changeKeyCode = key;
        }

        public void SendChangeInRewired()
        {
            ReInput.players.GetPlayers(true)[1].controllers.maps.GetFirstMapInCategory(0, 1, 1).ReplaceElementMap(map.id, map.actionId, map.axisContribution, changeKeyCode, ModifierKey.None, ModifierKey.None, ModifierKey.None);
        }
        
        public bool Update()
        {
            if (map.keyCode != changeKeyCode)
            {
                SetKey(map.keyCode);
                return true;
            }
            return false;
        }
    }

    [System.Serializable]
    public class KeyUIItem
    {
        public void Init(tk2dBaseSprite activator)
        {
            this.activator = activator;
            textMesh = uIItem.GetComponent<tk2dTextMesh>();
            uIItem.sendMessageTarget = instance.gameObject;
        }

        public tk2dUIItem uIItem;
        public tk2dBaseSprite background;
        public TweenBlink animator;

        public tk2dTextMesh textMesh    { get; private set; }
        public tk2dBaseSprite activator { get; private set; }
    }

    #endregion

    #region Values

    private delegate bool Check(float time);
    public static UserRemapKeyboard instance { get; private set; }
    private const float ASSIGMENT_TIMEOUT = 5.0f;
    private float timer = 0;
    private bool remapInProcess = false;
    private string playerPrefsBaseKey = "";
    private Color baseColorBackground;
    private Color inRemapColorBackground;
    private Color baseFrameColor;
    private Color inRemapFrameColor;
    
    public Actions actions = new Actions();
    public KeyboardUIItem keyboardItemPrefab;
    public tk2dUIScrollableArea scrollableArea;

    #endregion

    #region Inint/Destroy

    private void OnEnable()
    {
        if (remapInProcess)
        {
            actions.Deselect();
            remapInProcess = false;
        }
        actions.DiscardChange();

        if (instance != null) return;
        instance = this;
        playerPrefsBaseKey = GameData.CurrentGame.ToString();
        switch (GameData.CurrentGame)
        {
            case Game.IronTanks:
                inRemapFrameColor = new Color(0.99f, 0.54f, 0.14f, 1f);
                inRemapColorBackground = new Color(0f, 0.32f, 0.52f, 1f);

                baseFrameColor = new Color(0, 0.125f, 0.26f, 1f);
                baseColorBackground = new Color(0, 0.125f, 0.26f, 1f);
                break;
            case Game.FutureTanks:
                inRemapFrameColor = new Color(0.55f, 0.83f, 1f, 0.7f);
                inRemapColorBackground = new Color(0.027f, 0.07f, 0.07f, 1f);

                baseFrameColor = new Color(0.027f, 0.07f, 0.098f, 0.3f);
                baseColorBackground = new Color(0.027f, 0.07f, 0.098f, 0.4f);
                break;
            case Game.ToonWars:
                inRemapFrameColor = new Color(0.35f, 0.12f, 0f, 1f);
                inRemapColorBackground = new Color(0.35f, 0.12f, 0f, 1f);

                baseFrameColor = new Color(0.58f, 0.2f, 0.09f, 1f);
                baseColorBackground = new Color(0.58f, 0.2f, 0.09f, 1f);
                break;
            case Game.SpaceJet:
            case Game.ApocalypticCars:
            case Game.BattleOfWarplanes:
            case Game.BattleOfHelicopters:
            case Game.WWR:
            case Game.Armada:
            default:
                inRemapColorBackground = new Color(0.51f, 0.69f, 0.69f, 0.6f);
                baseColorBackground = Color.black;
                break;
        }

        IList<ActionElementMap> actionElementMapsDefault = ReInput.players.GetPlayers(true)[1].controllers.maps.GetFirstMapInCategory(0, 1, 1).ButtonMaps;//.AllMaps
        LoadAllMaps();
        actions.Init(actionElementMapsDefault);
    }

    private void OnDestroy()
    {
        instance = null;
    }

    #endregion

    #region ClickButton/Timer

    public void ClickButton(tk2dUIItem uiItem)
    {
        if (remapInProcess) return;
        remapInProcess = true;
        actions.Select(uiItem);
        timer = 0;
        StartCheck(0.05f, CheckInput);
    }

    private void StartCheck(float time, Check check) { StartCoroutine(CheckCoroutine(time, check)); }
    private IEnumerator CheckCoroutine(float time, Check check) { while (check(time)) { yield return new WaitForSeconds(time); } }
    
    private bool CheckInput(float time) //TimeWait
    {
        timer += time;
        if (timer >= ASSIGMENT_TIMEOUT)
        {
            actions.Deselect();
            remapInProcess = false;
            return false;
        }

        foreach (ControllerPollingInfo info in ReInput.controllers.Keyboard.PollForAllKeys())
        {
            if (info.keyboardKey != KeyCode.None)
            {
                actions.ChangeKey(info.keyboardKey);
                remapInProcess = false;
                return false;
            }
        }
        return true;
    }

    #endregion

    #region Load/Save

    public void LoadAllMaps()
    {
        //remapInProcess = false;
        Player player = ReInput.players.GetPlayers(true)[1];
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

    public void ResetOnDefault()
    {
        ReInput.players.GetPlayers(true)[1].controllers.maps.LoadDefaultMaps(ControllerType.Keyboard);
        remapInProcess = false;
        actions.Update();
    }

    public void SaveAllMaps()
    {
        remapInProcess = false;
        actions.SendChanges();
        Player player = ReInput.players.GetPlayers(true)[1];
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
        string key = playerPrefsBaseKey;
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
}