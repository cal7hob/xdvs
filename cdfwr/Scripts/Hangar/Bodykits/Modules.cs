using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Module
{
    public TankModuleInfos.ModuleType type;
    public int level;
    public GameObject gameObject;

    private const float OUTLINE_WIDTH = 0.0025f;
    private const string NORMAL_SHADER = "Mobile/Bumped Specular - 3 Color RGBMask";
    private const string OUTLINE_SHADER = "Mobile/Bumped Specular - 3 Color RGBMask OutLine";
    private const string OUTLINE_SHADER2 = "Mobile/Diffuse - ColoredMask AlphaMask OutLine";

    private static readonly Color OUTLINE_COLOR = new Color(0.92f, 0.54f, 0.16f, 1.0f);

    private Material cachedDefaultMaterial;
    private Material cachedOutlineMaterial;

    public static Module Parse(Transform transform)
    {
        var result = new Module();

        string[] nameParts = transform.name.Split('_');

        switch (nameParts[1])
        {
            case "armour":
            case "armor":
                result.type = TankModuleInfos.ModuleType.Armor;
                break;
            case "engine":
                result.type = TankModuleInfos.ModuleType.Engine;
                break;
            case "gun":
                result.type = TankModuleInfos.ModuleType.Cannon;
                break;
            case "recharge":
                result.type = TankModuleInfos.ModuleType.Reloader;
                break;
            case "shield":
                result.type = TankModuleInfos.ModuleType.Tracks;
                break;
        }

        result.level = char.ToUpper(nameParts[3][0]) - 64;
        result.gameObject = transform.gameObject;

        return result;
    }

    public bool CheckOwnship(int vehicleId)
    {
        // TODO: пока что только для своей техники. Надо сделать то же для клонов.

        VehicleUpgrades upgrades;

        if (ProfileInfo.vehicleUpgrades.TryGetValue(vehicleId, out upgrades))
            return upgrades.ModuleLevels[type] >= level;

        return false;
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }

    public void SetOutline(bool enabled)
    {
        Renderer renderer = gameObject.GetComponent<MeshRenderer>();

        if (enabled)
        {
            renderer.material = GetOutlineMaterial(renderer);
        }
        else
        {
            renderer.material = cachedDefaultMaterial ?? renderer.material;
        }
    }

    public void SetOutlineAlpha(float value)
    {
        if (cachedOutlineMaterial != null)
        {
            cachedOutlineMaterial.SetColor("_OutlineColor", new Color(OUTLINE_COLOR.r, OUTLINE_COLOR.g, OUTLINE_COLOR.b, value));
        }
    }

    private Material GetOutlineMaterial(Renderer renderer) // TODO: отрефакторить, херня какая-то.
    {
        if (cachedOutlineMaterial != null)
        {
            return cachedOutlineMaterial;
        }

        cachedDefaultMaterial = renderer.material;

        string currentShaderName = renderer.sharedMaterial.shader.name;

        string targetShaderName = currentShaderName == NORMAL_SHADER ? OUTLINE_SHADER : OUTLINE_SHADER2;

        cachedOutlineMaterial = new Material(Shader.Find(targetShaderName));

        cachedOutlineMaterial.SetTexture("_MainTex", cachedDefaultMaterial.GetTexture("_MainTex"));
        cachedOutlineMaterial.SetColor("_OutlineColor", OUTLINE_COLOR);
        cachedOutlineMaterial.SetFloat("_Outline", OUTLINE_WIDTH);

        return cachedOutlineMaterial;
    }
}

public class Modules : MonoBehaviour
{
    public new Animation animation;
    public float outlineAlpha;
    public List<Module> modules;

    private Module activeModule;
    private VehicleController vehicleController;

    void Awake()
    {
        HangarModuleWindow.OnLevelChange += OnModuleLevelChanged;
        GUIPager.OnPageChange += OnPageChanged;

        Dispatcher.Subscribe(EventId.TankJoinedBattle, OnTankJoinedBattle);
        Dispatcher.Subscribe(EventId.HangarVehicleGeometryLoaded, OnHangarVehicleGeometryLoaded);
        Dispatcher.Subscribe(EventId.VehicleSelected, OnVehicleSelected);

        vehicleController = GetComponentInParent<VehicleController>();
    }

    void OnDestroy()
    {
        HangarModuleWindow.OnLevelChange -= OnModuleLevelChanged;
        GUIPager.OnPageChange -= OnPageChanged;

        Dispatcher.Unsubscribe(EventId.TankJoinedBattle, OnTankJoinedBattle);
        Dispatcher.Unsubscribe(EventId.HangarVehicleGeometryLoaded, OnHangarVehicleGeometryLoaded);
        Dispatcher.Unsubscribe(EventId.VehicleSelected, OnVehicleSelected);
    }

    void Update()
    {
        if (activeModule != null && animation.isPlaying)
        {
            activeModule.SetOutlineAlpha(outlineAlpha);
        }
    }

    private void OnModuleLevelChanged(TankModuleInfos.ModuleType type, int level)
    {
        foreach (Module module in modules)
        {
            bool moduleSelected = module.type == type && module.level == level;

            module.SetActive(moduleSelected || module.CheckOwnship(Shop.CurrentVehicle.Info.id));
            module.SetOutline(moduleSelected);

            if (moduleSelected)
            {
                activeModule = module;
            }
        }
    }

    private void OnPageChanged(string previousPage, string currentPage)
    {
        if (currentPage == "Armory")
        {
            animation.Play();
        }
        else
        {
            activeModule = null;
            animation.Stop();
            ShowModules(Shop.CurrentVehicle.Info.id);
        }
    }

    private void OnTankJoinedBattle(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;
        int playerId = info.int1;

        if (vehicleController.data.playerId == playerId && vehicleController.IsMain)
        {
            ShowModules(vehicleController.id);
        }
    }

    private void OnHangarVehicleGeometryLoaded(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;
        int vehicleId = info.int1;
        ShowModules(vehicleId);
    }

    private void OnVehicleSelected(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;
        int vehicleId = info.int1;
        ShowModules(vehicleId);
    }

    private void ShowModules(int vehicleId)
    {
        foreach (Module module in modules)
        {
            module.SetActive(module.CheckOwnship(vehicleId));
        }
    }
}
