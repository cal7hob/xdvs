using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PanelBtnsSwitcher : MonoBehaviour
{

    public GameObject mainMenuWrapper;
    public tk2dSlicedSprite topPanelCover;
    public List<UIButton> hangarBtnsList;       // тут списки с кнопками и плашками, для которых иногда надо делать "дырки" в blackAlphaLayer`е
    public List<UIField> hangarFieldsList; 

    public Dictionary<string, UIButton> hangarBtnsDict;
    public Dictionary<string, UIField> hangarFieldsDict;

    public static PanelBtnsSwitcher Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        hangarBtnsDict = new Dictionary<string, UIButton>();
        hangarFieldsDict = new Dictionary<string, UIField>();

        foreach (var btn in hangarBtnsList)
        {
            hangarBtnsDict.Add(btn.name, btn);
        }

        foreach (var field in hangarFieldsList)
        {
            hangarFieldsDict.Add(field.name, field);
        }
    }

    void OnDestroy()
    {
        Instance = null;
    }

    public static void SetActiveBtn(string btnName, bool activate)
    {
        var btn = Instance.hangarBtnsDict[btnName];

        btn.boxCollider.enabled = activate;
        btn.depthMask.gameObject.SetActive(activate);
    }

    public static void SetFieldVisibleThruBlackLayer(string fieldName, bool activate)
    {
        var field = Instance.hangarFieldsDict[fieldName];

        field.depthmask.gameObject.SetActive(activate);
    }

    public static void SetActiveAllBtns(bool activate)
    {
        foreach (var btn in Instance.hangarBtnsList)
        {
            btn.boxCollider.enabled = activate;
        }

        ScoresController.Instance.btnToggleArrow.parent.GetComponent<BoxCollider>().enabled = activate;
    }

    public static void SetActiveJustThisBtn(string btnName, bool activate)
    {
        SetActiveAllBtns(!activate);
        SetActiveBtn(btnName, activate);
    }
}
