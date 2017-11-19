using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PanelSwitch : MonoBehaviour
{
    [Serializable]
    public class PanelOpts
    {
        public string key;
        public string plainLabel; // Без локализации
        public string localeLabelKey;
        public string secondLabel;
        public List<GameObject> hideOnOpen;
        public List<PanelSwitch> hideAdvancedPanelsOnOpen;
        public bool isHided = false;
    }

    [Serializable]
    public class AdvancedPanelOpts
    {
        public GameObject panel;
        public LabelLocalizationAgent label;
    }

    [Serializable]
    public class PanelButton
    {
        public tk2dUIItem button;
        public tk2dSprite sprite;
        [Header("Change color of one sprite to disable btn...")]
        public bool useColorsForStateControl = false;
        public Color colorEnabled = new Color32(255, 255, 255, 255);
        public Color colorDisabled = new Color32(255, 255, 255, 50);
        [Header("or use ActivatedUpDownButton for it")]
        public ActivatedUpDownButton activatedScript;

        public void SetEnabled(bool flag = true)
        {
            if (useColorsForStateControl)
            {
                button.gameObject.SetActive(true);

                if (sprite == null)
                {
                    sprite = button.GetComponent<tk2dSprite>();
                }

                sprite.color = flag ? colorEnabled : colorDisabled;
            }
            else if(activatedScript)
            {
                activatedScript.Activated = flag;
            }
            else
            {
                button.gameObject.SetActive(flag);
            }
        }
    }

    public delegate void PanelChangedDelegate(int index, string key);

    public PanelButton buttonNext;
    public PanelButton buttonPrevious;
    public LabelLocalizationAgent panelLabel;

    public AudioClip clickSound;

    public int initialPanelIndex = 0;
    public PanelOpts CurrentPanel
    {
        get
        {
            PanelsInit();
            return panels[m_showPanels[m_currentPanel]];
        }
    }
    public PanelOpts[] panels;
    public AdvancedPanelOpts advancedPanel;
    public bool isAdvancedPanelHided = false;

    public tk2dUIScrollableArea areaForSwipeControl;
    public bool inverseSwipe = false;

    public PanelChangedDelegate OnPanelChanged;

    [SerializeField]
    private int m_currentPanel = 0;
    private bool m_wasSwipe = false;
    private List<int> m_showPanels = new List<int>();

    private bool m_isPanelsInit = false;

    void PanelsInit()
    {
        if (!m_isPanelsInit)
        {
            for (int i = 0; i < panels.Length; i++)
            {
                if (!panels[i].isHided)
                {
                    m_showPanels.Add(i);
                }
            }

            m_currentPanel = m_showPanels.IndexOf(initialPanelIndex);

            if (m_currentPanel < 0)
            {
                m_currentPanel = 0;
            }

            m_isPanelsInit = true;
        }
    }
    
    void Awake()
    {
        PanelsInit();

        buttonNext.button.OnClick += ButtonNextClicked;
        buttonPrevious.button.OnClick += ButtonPreviousClicked;

        if (areaForSwipeControl != null)
        {
            areaForSwipeControl.OnScroll += OnAreaScrolled;
            areaForSwipeControl.backgroundUIItem.OnDown += OnDown;
            areaForSwipeControl.backgroundUIItem.OnUp += OnUp;

            if (GetComponent<tk2dUILayout>())
            {
                GetComponent<tk2dUILayout>().OnReshape += OnReshaped;
            }
        }

        panelLabel.Parameter = "";
    }

    void OnDestroy()
    {
        buttonNext.button.OnClick -= ButtonNextClicked;
        buttonPrevious.button.OnClick -= ButtonPreviousClicked;

        if (areaForSwipeControl != null)
        {
            areaForSwipeControl.OnScroll -= OnAreaScrolled;
            areaForSwipeControl.backgroundUIItem.OnDown -= OnDown;
            areaForSwipeControl.backgroundUIItem.OnUp -= OnUp;

            if (GetComponent<tk2dUILayout>())
            {
                GetComponent<tk2dUILayout>().OnReshape -= OnReshaped;
            }
        }
    }

    public void SetLabelForKey(string key, string label)
    {
        foreach (var p in panels)
        {
            if (p.key != key)
                continue;

            p.plainLabel = label;

            if (p == CurrentPanel)
            {
                panelLabel.transform.GetComponent<tk2dTextMesh>().text = label;

                if (advancedPanel.panel != null)
                    advancedPanel.label.Parameter = label;
            }

            break;
        }
    }

    public void SetSecondLabelForKey(string key, string label)
    {
        foreach (var p in panels)
        {
            if (p.key == key)
            {
                p.secondLabel = label;

                if (p == CurrentPanel)
                {
                    panelLabel.Parameter = label;

                    if (advancedPanel.panel != null)
                        advancedPanel.label.Parameter = label;
                }

                break;
            }
        }
    }

    public void hide(string key)
    {
        int ind = -1;
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i].key == key)
            {
                ind = i;
                break;
            }
        }
        if (ind == -1)
        {
            return;
        }
        if (panels[ind].isHided)
        {
            return;
        }
        panels[ind].isHided = true;
        bool isPanelChanged = (ind == m_showPanels[m_currentPanel]);
        if (isPanelChanged)
        {
            BeforePanelChange(false);
        }
        m_showPanels.Remove(ind);
        if (m_currentPanel >= m_showPanels.Count())
        {
            m_currentPanel = m_showPanels.Count() - 1;
        }
        if (isPanelChanged)
        {
            AfterPanelChanged();
        }
    }

    public void switchTo(string key)
    {
        int ind = -1;
        for (int i = 0; i < m_showPanels.Count(); i++)
        {
            if (panels[m_showPanels[i]].key == key)
            {
                ind = i;
                break;
            }
        }
        if (ind == -1)
        {
            return;
        }
        BeforePanelChange();
        m_currentPanel = ind;
        AfterPanelChanged();
    }


    void ButtonNextClicked()
    {
        if (m_currentPanel < m_showPanels.Count() - 1)
        {
            BeforePanelChange();
            m_currentPanel++;
            AfterPanelChanged();
        }
    }

    void ButtonPreviousClicked()
    {
        if (m_currentPanel > 0)
        {
            BeforePanelChange();
            m_currentPanel--;
            AfterPanelChanged();
        }
    }

    void BeforePanelChange(bool doPlaySound = true)
    {
        foreach (var toRestoreHiddenBefore in CurrentPanel.hideOnOpen)
        {
            if (toRestoreHiddenBefore != null)
            {
                if (toRestoreHiddenBefore == advancedPanel.panel && isAdvancedPanelHided)
                    continue;

                toRestoreHiddenBefore.SetActive(true);
            }
        }

        foreach (var toRestoreHiddenBefore in CurrentPanel.hideAdvancedPanelsOnOpen)
        {
            if (toRestoreHiddenBefore != null)
            {
                toRestoreHiddenBefore.isAdvancedPanelHided = false;

                if (toRestoreHiddenBefore.CurrentPanel.hideOnOpen.Contains(toRestoreHiddenBefore.advancedPanel.panel))
                    continue;

                toRestoreHiddenBefore.advancedPanel.panel.SetActive(true);
                toRestoreHiddenBefore.panelLabel.gameObject.SetActive(false);
            }
        }

        if (doPlaySound)
        {
            AudioDispatcher.PlayClip(clickSound, false);
        }
    }

    void AfterPanelChanged()
    {
        if (panelLabel.key != CurrentPanel.localeLabelKey)
        {
            panelLabel.key = CurrentPanel.localeLabelKey;
            if (advancedPanel.label != null)
                advancedPanel.label.key = CurrentPanel.localeLabelKey;

            if (!string.IsNullOrEmpty(CurrentPanel.secondLabel))
            {
                panelLabel.Parameter = CurrentPanel.secondLabel;
                if (advancedPanel.label != null)
                    advancedPanel.label.Parameter = CurrentPanel.secondLabel;
            }
            else
            {
                if (string.IsNullOrEmpty(CurrentPanel.plainLabel))
                    panelLabel.LocalizeLabel();
                else
                {
                    panelLabel.gameObject.GetComponent<tk2dTextMesh>().text = CurrentPanel.plainLabel;
                }

                if (advancedPanel.label != null)
                    advancedPanel.label.LocalizeLabel();
            }
        }

        foreach (var toHide in CurrentPanel.hideOnOpen)
        {
            if (toHide != null)
                toHide.SetActive(false);
        }

        foreach (var panelToHide in CurrentPanel.hideAdvancedPanelsOnOpen)
        {
            panelToHide.advancedPanel.panel.SetActive(false);
            panelToHide.panelLabel.gameObject.SetActive(true);
            panelToHide.isAdvancedPanelHided = true;
        }

        buttonNext.SetEnabled(m_currentPanel < m_showPanels.Count() - 1);
        buttonPrevious.SetEnabled(m_currentPanel > 0);

        if (advancedPanel.panel != null && advancedPanel.panel.activeSelf)
            panelLabel.gameObject.SetActive(false);
        else
            panelLabel.gameObject.SetActive(true);

        if (OnPanelChanged != null)
        {
            OnPanelChanged(m_currentPanel, CurrentPanel.key);
        }
    }

    void OnAreaScrolled(tk2dUIScrollableArea area)
    {
        m_wasSwipe = true;
    }

    void OnDown()
    {
        m_wasSwipe = false;
    }

    void OnUp()
    {
        if (m_wasSwipe)
        {
            m_wasSwipe = false;
            if (areaForSwipeControl.Value == (inverseSwipe ? 0f : 1f))
            {
                ButtonPreviousClicked();
            }
            else
            {
                ButtonNextClicked();
            }
        }
    }

    void OnReshaped(Vector3 dMin, Vector3 dMax)
    {
        areaForSwipeControl.ContentLength = areaForSwipeControl.VisibleAreaLength + 1f;
    }
}
