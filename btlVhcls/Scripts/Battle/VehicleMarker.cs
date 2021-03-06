﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VehicleMarker : MonoBehaviour
{
    private const string FIRST_REGULAR_SHADER = "Mobile/Bumped Specular - 3 Color RGBMask NoAlpha";
    private const string FIRST_OUTLINE_SHADER = "Mobile/Bumped Specular - 3ColorRGBMaskNoAlphaOutline";
    private const string SECOND_OUTLINE_SHADER = "Mobile/Diffuse - ColoredMask AlphaMaskNoAlpha Outline";

    private List<Renderer> vehicleRenderers;
    private Dictionary<Renderer, Material> normalMaterials;
    private Dictionary<Renderer, Material> markedMaterials;
    private Shader markedShader1;
    private Shader markedShader2;
    private bool marked;
    private bool initialized;
    private float outlineKoef = 1f;

    private static readonly Color OUTLINE_COLOR = new Color(1f, 0, 0, 1f);
    private const float NORM_OUTLINE_WIDTH = 0.04f;
    private const float ZOOMED_OUTLINE_WIDTH = 0.01f;

    void Awake()
    {
        StartCoroutine(Initializing());
#if NETFX_CORE || UNITY_WSA
        outlineKoef = 0.5f;
#elif UNITY_IOS
        if (MiscTools.IsDeviceAppleIphone ()) {
            outlineKoef = 50f;
        }
#endif
    }

    public void SetMarkedStatus(bool status)
    {
        if (!initialized || status == marked || vehicleRenderers == null)
            return;

        for (int i = 0; i < vehicleRenderers.Count; i++)
        {
            Renderer rend = vehicleRenderers[i];
            rend.material = status ? markedMaterials[rend] : normalMaterials[rend];
        }

        marked = status;
    }

    private IEnumerator Initializing()
    {
        yield return new WaitForSeconds(0.5f);
        Dispatcher.Subscribe(EventId.ZoomStateChanged, OnZoomStateChanged);
        VehicleController vehicle = GetComponentInChildren<VehicleController>();
        if (vehicle == null)
        {
            Debug.LogError("VehicleOutliner: There is no VehicleController to interact", gameObject);
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

        normalMaterials = new Dictionary<Renderer, Material>(vehicleRenderers.Count);
        markedMaterials = new Dictionary<Renderer, Material>(vehicleRenderers.Count);
        foreach (Renderer rend in vehicleRenderers)
        {
            string regularShaderName = rend.sharedMaterial.shader.name;
            normalMaterials.Add(rend, rend.sharedMaterial);
            Material markedMaterial = new Material(regularShaderName == FIRST_REGULAR_SHADER ? markedShader1 : markedShader2);
            markedMaterial.CopyPropertiesFromMaterial(rend.sharedMaterial);
            markedMaterial.SetColor("_OutlineColor", OUTLINE_COLOR);
            markedMaterial.SetFloat("_Outline", NORM_OUTLINE_WIDTH * outlineKoef);
            markedMaterials.Add(rend, markedMaterial);
        }

        initialized = true;
    }

    private void OnZoomStateChanged(EventId id, EventInfo ei)
    {
        bool zoomed = BattleCamera.Instance.IsZoomed;
        foreach (var markedMat in markedMaterials.Values)
        {
            markedMat.SetFloat("_Outline", (zoomed ? ZOOMED_OUTLINE_WIDTH : NORM_OUTLINE_WIDTH) * outlineKoef);
        }
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.ZoomStateChanged, OnZoomStateChanged);
    }
}