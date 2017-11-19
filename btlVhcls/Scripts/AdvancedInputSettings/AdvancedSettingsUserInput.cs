using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AdvancedSettingsUserInput : HangarPage
{
    //!!!!! Порядок ссылок в tk2dUIToggleButtonGroup должен быть такой же как в этом енаме !!!!!
    public enum Tab
    {
        Touch = 0,
        KeyRemap = 1,
    }

    [SerializeField] private tk2dUIToggleButtonGroup tabs;
    [SerializeField] private GameObject[] pages;//порядок как в енаме Tab

    [Header("Touch")]
    [SerializeField] private tk2dUIScrollbar TurretSensibility;
    [SerializeField] private tk2dUIScrollbar MoveXSensibility;
    [SerializeField] private tk2dUIScrollbar MoveYSensibility;
    [SerializeField] private tk2dUIScrollbar MoveXDeadZone;
    [SerializeField] private tk2dUIScrollbar MoveYDeadZone;
    [SerializeField] private tk2dUIToggleButtonGroup typeTurret;

    [Header("KeyRemap")]
    [SerializeField] private UserRemapKeyboard panelKeyRemap;


    protected override void Create()
    {
        base.Create();

        if(Rewired.ReInput.isReady && ProfileInfo.IsBattleTutorialCompleted)
            panelKeyRemap.LoadAllMaps();
    }

    protected override void Destroy()
    {
        base.Destroy();
    }

    protected override void Show()
    {
        base.Show();

        SetTab(GUIPager.ActivePage.WindowData != null && GUIPager.ActivePage.WindowData.ContainsKey("SetTab") ? 
            (Tab)GUIPager.ActivePage.WindowData.GetValue("SetTab") :
            Tab.Touch);
    }

    public void SubmitForAdvancedInput(tk2dUIItem btn)
    {
        SaveParams();
        GUIPager.Back();
    }

    private void SaveParams()
    {
        if (TurretSensibility)
            PlayerPrefs.SetFloat("TurretRotationSensitivity", TurretSensibility.Value);
        if (MoveXSensibility)
            PlayerPrefs.SetFloat("MoveJXSensibility", MoveXSensibility.Value);
        if (MoveYSensibility)
            PlayerPrefs.SetFloat("MoveJYSensibility", MoveYSensibility.Value);
        if (MoveXDeadZone)
            PlayerPrefs.SetFloat("MoveJDeadZoneX", MoveXDeadZone.Value);
        if (MoveYDeadZone)
            PlayerPrefs.SetFloat("MoveJDeadZoneY", MoveYDeadZone.Value);
        if (typeTurret)
            PlayerPrefs.SetInt("TurretControlType", typeTurret.SelectedIndex);
    }

    public void ResetToDefault()
    {
        PlayerPrefs.SetFloat("TurretRotationSensitivity", Settings.DEFAULT_TURRET_ROTATION_SENSITIVITY);
        PlayerPrefs.SetFloat("MoveJXSensibility", 0.5f);
        PlayerPrefs.SetFloat("MoveJYSensibility", 0.5f);
        PlayerPrefs.SetFloat("MoveJDeadZoneX", 0.07f);
        PlayerPrefs.SetFloat("MoveJDeadZoneY", 0.3f);
        PlayerPrefs.SetInt("TurretControlType", 0);
        
        ProfileChanged();
    }

    protected override void ProfileChanged()
    {
        base.ProfileChanged();

        if (TurretSensibility)
            TurretSensibility.Value = PlayerPrefs.GetFloat("TurretRotationSensitivity", Settings.DEFAULT_TURRET_ROTATION_SENSITIVITY);
        if (MoveXSensibility)
            MoveXSensibility.Value = PlayerPrefs.GetFloat("MoveJXSensibility", 0.5f);
        if (MoveYSensibility)
            MoveYSensibility.Value = PlayerPrefs.GetFloat("MoveJYSensibility", 0.5f);
        if (MoveXDeadZone)
            MoveXDeadZone.Value = PlayerPrefs.GetFloat("MoveJDeadZoneX", 0.07f);
        if (MoveYDeadZone)
            MoveYDeadZone.Value = PlayerPrefs.GetFloat("MoveJDeadZoneY", 0.3f);
        if (typeTurret)
            typeTurret.SelectedIndex = PlayerPrefs.GetInt("TurretControlType", 0);
    }

    public void OnTabChanged(tk2dUIToggleButtonGroup buttonGroup)
    {
        for (int i = 0; i < pages.Length; i++)
            pages[i].SetActive(tabs.SelectedIndex == i);
    }

    public void SetTab(Tab _tab)
    {
        if (((int)_tab) != tabs.SelectedIndex)
            tabs.SelectedIndex = (int)_tab;
    }
}
