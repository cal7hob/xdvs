using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Rewired;
using System.Linq;


public class KeyRemapItem : MonoBehaviour, IItem
{
    public class Data
    {
        public enum ButtonType
        {
            General = 0,
            Alternative = 1,
        }

        public string Name
        {
            get {
                string s =  InputAction.type == InputActionType.Button ? 
                    InputAction.name :
                    (AxisContribution == Pole.Positive ?
                        InputAction.positiveDescriptiveName :   
                        InputAction.negativeDescriptiveName);
                s = s.Replace(" ", "");
                return s;
            }
        }
        public int Index { get; private set; }
        public InputAction InputAction { get; private set; }
        public Pole AxisContribution { get; private set; }
        public string LocKey { get { return string.Format("RewiredAction_{0}", Name); } }
        public List<ActionElementMap> ActionElementMaps { get; private set; }

        public Data(int index, InputAction inputAction, Pole axisContribution, List<ActionElementMap> actionElementMapId)
        {
            Index = index;
            InputAction = inputAction;
            AxisContribution = axisContribution;
            ActionElementMaps = actionElementMapId;
        }

        public ActionElementMap GetButtonActionElementMap(ButtonType btnType)
        {
            if (ActionElementMaps == null)
                return null;
            int index = (int)btnType;
            if (ActionElementMaps.Count <= index)
                return null;
            return ActionElementMaps[index];
        }
    }

    [SerializeField] private tk2dSlicedSprite sizeBg;
    [SerializeField] private tk2dTextMesh actionName;
    [SerializeField] private tk2dTextMesh lblGeneralText;
    [SerializeField] private tk2dTextMesh lblAlternativeText;
    [SerializeField] private TweenBlink tweener;
    [SerializeField] private ActivationScript commonActivator;// бэк на всю строчку
    [SerializeField] private ActivationScript[] itemsActivators;//включать персональный бэк на каждый Data.BtnType при редактировании
    
    public bool IsRemapInProcess { get; private set; }

    private const float ASSIGMENT_TIMEOUT = 5.0f;

    private UserRemapKeyboard parentPanel;
    private float clickTime = 0;
    private tk2dUIItem remapUiitem;
    private Data.ButtonType remapBtnType;

    private Data data;

    public void Initialize(object[] parameters)
    {
        data = (Data)parameters[0];
        ParamDict additionalParams = (ParamDict)parameters[1];
        parentPanel = (UserRemapKeyboard)additionalParams.GetValue("parentPanel");
        gameObject.name = data.Name;

        UpdateElements();
    }

    public void UpdateElements()
    {
        actionName.text = Localizer.ContainsKey(data.LocKey) ? Localizer.GetText(data.LocKey) : data.Name;
        if(commonActivator)
            commonActivator.Activated = IsRemapInProcess;

        MiscTools.SetActiveActivatedObjectsCollection(itemsActivators, false);

        if (IsRemapInProcess)
        {
            remapUiitem.GetComponent<tk2dTextMesh>().text = "_";
            tweener.objectsToAnimate = new GameObject[] { remapUiitem.gameObject };
            tweener.SetActiveAnimation(true);
            if (itemsActivators != null && itemsActivators.Length > (int)remapBtnType)
                itemsActivators[(int)remapBtnType].Activated = true;
        }
        else
        {
            tweener.SetActiveAnimation(false);
            UpdateLabelKeycode(lblGeneralText, Data.ButtonType.General);
            UpdateLabelKeycode(lblAlternativeText, Data.ButtonType.Alternative);
        }
    }

    private void UpdateLabelKeycode(tk2dTextMesh lbl, Data.ButtonType btnType)
    {
        int intBtnType = (int)btnType;
        lbl.text = data.ActionElementMaps != null && data.ActionElementMaps.Count > intBtnType && data.ActionElementMaps[intBtnType].keyCode != KeyCode.None
                ? data.ActionElementMaps[intBtnType].keyCode.ToString() 
                : Localizer.GetText("lblEmpty");
    }

    private void Update()
    {
        if (!IsRemapInProcess)
            return;

        if (Time.realtimeSinceStartup - clickTime >= ASSIGMENT_TIMEOUT) //Прекращаем редактирование по бездействию
        {
            IsRemapInProcess = false;
            UpdateElements();
        }
        else //Или слушаем нажатую кнопку
        {
            foreach (ControllerPollingInfo info in ReInput.controllers.Keyboard.PollForAllKeys())
            {
                if (info.keyboardKey != KeyCode.None)
                {
                    IsRemapInProcess = false;
                    parentPanel.Remap(data, remapBtnType, info.keyboardKey);
                    //Debug.LogErrorFormat("Button to assign <{0}>", info.keyboardKey);
                    break;
                }
            }
        }
    }

    private void OnDisable()
    {
        if (IsRemapInProcess)//заканчиваем редактирование при выходе со вкладки / страницы
            IsRemapInProcess = false;
    }

    private void OnClickGeneralButton(tk2dUIItem btn)
    {
        OnClickKeymapBtn(Data.ButtonType.General, btn);
    }

    private void OnClickAlternativeButton(tk2dUIItem btn)
    {
        OnClickKeymapBtn(Data.ButtonType.Alternative, btn);
    }

    private void OnClickKeymapBtn(Data.ButtonType btnType, tk2dUIItem btn)
    {
        if (parentPanel.IsAnyItemRemapInProcess())
            return;

        clickTime = Time.realtimeSinceStartup;
        IsRemapInProcess = true;
        remapUiitem = btn;
        remapBtnType = btnType;
        UpdateElements();
    }

    private void SetupColors()
    {
        //if (GameData.IsGame(Game.FutureTanks))
        //{
        //    inRemapFrameColor = new Color(0.55f, 0.83f, 1f, 0.7f);
        //    inRemapColorBackground = new Color(0.027f, 0.07f, 0.07f, 1f);

        //    baseFrameColor = new Color(0.027f, 0.07f, 0.098f, 0.3f);
        //    baseColorBackground = new Color(0.027f, 0.07f, 0.098f, 0.4f);
        //}
        //if (GameData.IsGame(Game.ToonWars))
        //{
        //    inRemapFrameColor = new Color(0.35f, 0.12f, 0f, 1f);
        //    inRemapColorBackground = new Color(0.35f, 0.12f, 0f, 1f);

        //    baseFrameColor = new Color(0.58f, 0.2f, 0.09f, 1f);
        //    baseColorBackground = new Color(0.58f, 0.2f, 0.09f, 1f);
        //}
    }

    public void DesrtoySelf()
    {
    }

    public Vector2 GetSize()
    {
        return new Vector2(sizeBg.dimensions.x * sizeBg.scale.x, sizeBg.dimensions.y * sizeBg.scale.y);
    }

    public string GetUniqId { get { return data.Index.ToString(); } }

    public tk2dUIItem MainUIItem { get { return null; } }

    public Transform MainTransform { get { return transform; } }
}
