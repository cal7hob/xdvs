using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AdvancedSettingsUserInput : HangarPage
{
    [Header("Touch")]
    public tk2dUIScrollbar TurretSensibility;
    public tk2dUIScrollbar MoveXSensibility;
    public tk2dUIScrollbar MoveYSensibility;
    public tk2dUIScrollbar MoveXDeadZone;
    public tk2dUIScrollbar MoveYDeadZone;
    public tk2dUIToggleButtonGroup typeTurret;
    public List<SettingsPages> settingsPages = new List<SettingsPages>();

    protected override void Create()
    {
        base.Create();
    }

    protected override void Destroy()
    {
        base.Destroy();
    }

    public void OpenPage()
    {
        GUIPager.SetActivePage("AdvancedInputSettings", false, true);
        settingsPages[0].SettingsPageBtn.SimulateClick();
    }

    public void SubmitForAdvancedInput(tk2dUIItem uiItem)
    {
        SaveParams();
        GUIPager.Back();
    }

    private void SaveParams()
    {
        if (TurretSensibility != null)
        {
            PlayerPrefs.SetFloat("TurretRotationSensitivity", TurretSensibility.Value);
        }
        if (MoveXSensibility != null)
        {
            PlayerPrefs.SetFloat("MoveJXSensibility", MoveXSensibility.Value);
        }
        if (MoveYSensibility != null)
        {
            PlayerPrefs.SetFloat("MoveJYSensibility", MoveYSensibility.Value);
        }
        if (MoveXDeadZone != null)
        {
            PlayerPrefs.SetFloat("MoveJDeadZoneX", MoveXDeadZone.Value);
        }
        if (MoveYDeadZone != null)
        {
            PlayerPrefs.SetFloat("MoveJDeadZoneY", MoveYDeadZone.Value);
        }
        if (typeTurret != null)
        {
            PlayerPrefs.SetInt("TurretControlType", typeTurret.SelectedIndex);
        }
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

        if (TurretSensibility != null)
        {
            TurretSensibility.Value = PlayerPrefs.GetFloat("TurretRotationSensitivity", Settings.DEFAULT_TURRET_ROTATION_SENSITIVITY);
        }
        if (MoveXSensibility != null)
        {
            MoveXSensibility.Value = PlayerPrefs.GetFloat("MoveJXSensibility", 0.5f);
        }
        if (MoveYSensibility != null)
        {
            MoveYSensibility.Value = PlayerPrefs.GetFloat("MoveJYSensibility", 0.5f);
        }
        if (MoveXDeadZone != null)
        {
            MoveXDeadZone.Value = PlayerPrefs.GetFloat("MoveJDeadZoneX", 0.07f);
        }
        if (MoveYDeadZone != null)
        {
            MoveYDeadZone.Value = PlayerPrefs.GetFloat("MoveJDeadZoneY", 0.3f);
        }
        if (typeTurret != null)
        {
            typeTurret.SelectedIndex = PlayerPrefs.GetInt("TurretControlType", 0);
        }

    }

    public void PageSelectionClick(tk2dUIItem item)
    {
        foreach (var page in settingsPages)
        {
            var currentPage = page.SettingsPage;
            var currentToggleBtn = page.SettingsPageBtn.GetComponent<tk2dUIToggleControl>();
            var showHideScript = currentPage.GetComponent<ShowHideGUIPage>();

            if (page.SettingsPageBtn == item)
            {
                if (showHideScript) showHideScript.MoveToDefaultPositionAndShow();
                else currentPage.SetActive(true);

                if (currentToggleBtn != null)
                {
                    currentToggleBtn.IsOn = true;
                }
            }
            else
            {
                if (showHideScript) showHideScript.Hide();
                else currentPage.SetActive(false);

                if (currentToggleBtn != null)
                {
                    currentToggleBtn.IsOn = false;
                }
            }
        }
    }
}
