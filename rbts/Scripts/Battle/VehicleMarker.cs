/*using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VehicleMarker : MonoBehaviour
{
    private const string FIRST_REGULAR_SHADER = "Mobile/Bumped Specular - 3 Color RGBMask NoAlpha";
    private const string FIRST_OUTLINE_SHADER = "Mobile/Bumped Specular - 3ColorRGBMaskNoAlphaOutline";
    private const string SECOND_OUTLINE_SHADER = "Mobile/Diffuse - ColoredMask AlphaMaskNoAlpha Outline";

    private List<Renderer> vehicleRenderers;
    private Dictionary<Material, Material> normalMaterials = new Dictionary<Material, Material>();
    private Dictionary<Material, Material> markedMaterials = new Dictionary<Material, Material>();
    private Shader markedShader1;
    private Shader markedShader2;
    private bool marked;
	private bool initialized;

    private static readonly Color OUTLINE_COLOR = new Color(1f, 0, 0, 1f);
    
/*    void Awake()
    {
        StartCoroutine(Initializing());
    }

    void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.ZoomStateChanged, OnZoomStateChanged);

        foreach (var kvp in markedMaterials)
        {
            Destroy(kvp.Value);
        }
    }
    #1#
    public void SetMarkedStatus(bool status)
    {
        /*if (!initialized || status == marked || vehicleRenderers == null)
            return;

        marked = status;
        for (int i = 0; i < vehicleRenderers.Count; i++)
        {
            Renderer rend = vehicleRenderers[i];
            rend.material = marked ? markedMaterials[rend.sharedMaterial] : normalMaterials[rend.sharedMaterial];
        }#1#
    }
    /*
    private IEnumerator Initializing()
    {
        yield return new WaitForSeconds(0.5f);
        Messenger.Subscribe(EventId.ZoomStateChanged, OnZoomStateChanged);
        VehicleController vehicle = GetComponentInChildren<VehicleController>();
        if (vehicle == null)
        {
            Debug.LogError("VehicleMarker: There is no VehicleController to interact", gameObject);
            Destroy(this);
            yield break;
        }

        vehicleRenderers = new List<Renderer>(GetComponentsInChildren<Renderer>(true));
        vehicleRenderers.RemoveAll(
            x => x.name.Contains("hadow")
            || x.GetComponent<StickerKit>() != null
            || x.GetComponent<ParticleSystem>() != null); // Пропускаем наклейки и эффекты
        markedShader1 = Shader.Find(FIRST_OUTLINE_SHADER);
        markedShader2 = Shader.Find(SECOND_OUTLINE_SHADER);

        foreach (Renderer rend in vehicleRenderers)
        {
            if (markedMaterials.ContainsKey(rend.sharedMaterial))
                continue;

            string regularShaderName = rend.sharedMaterial.shader.name;
            Material markedMaterial = new Material(regularShaderName == FIRST_REGULAR_SHADER ? markedShader1 : markedShader2);
            markedMaterial.CopyPropertiesFromMaterial(rend.sharedMaterial);
            markedMaterial.SetColor("_OutlineColor", OUTLINE_COLOR);
            markedMaterial.SetFloat("_Outline", GameSettings.Instance.OutlineWidth);

            normalMaterials.Add(markedMaterial, rend.sharedMaterial);
            markedMaterials.Add(rend.sharedMaterial, markedMaterial);
        }

		initialized = true;
    }

    private void OnZoomStateChanged(EventId id, EventInfo ei)
    {
        bool zoomed = BattleCamera.Instance.IsZoomed;
        foreach (var markedMat in markedMaterials.Values)
        {
            markedMat.SetFloat("_Outline", zoomed ? GameSettings.Instance.ZoomOutlineWidth : GameSettings.Instance.OutlineWidth);
        }
    }#1#
}*/