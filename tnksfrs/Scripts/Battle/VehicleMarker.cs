using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using XD;

public interface IVehicleMarker : ISubscriber
{
}

public class VehicleMarker : MonoBehaviour, IVehicleMarker
{
    #region ISubscriber      
    public string Description
    {
        get
        {
            return "[VehicleMarker] " + name;
        }

        set
        {
            name = value;
        }
    }

    public void Reaction(Message message, params object[] parameters)
    {
        switch (message)
        {
            case Message.Hit:
                if (hitRoutine != null)
                {
                    StopCoroutine(hitRoutine);
                }

                hitRoutine = StartCoroutine(HitRoutine(parameters.Get<float>()));
                break;
        }
    }
    #endregion    

    [SerializeField]
    private string                          normalShader = "Mobile/BumpSpec-3ColRGBMaskNoAlpha";
    [SerializeField]
    private string                          outlineShader = "Mobile/BumpSpec-3ColRGBMaskNoAlphaOutline";
    [SerializeField]
    private string                          outlineShader2 = "Mobile/Diffuse-3ColRGBMaskNoAlphaOutline";

    [SerializeField]
    private float                           normalOutline = 0.01f;
    [SerializeField]
    private float                           zoomedOutline = 0.002f;
    [SerializeField]
    private bool                            marked = false;
    [SerializeField]
    private Color                           color = Color.red;
    [SerializeField]
    private VehicleController               unit = null;
    [SerializeField]
    private MaterialsContainer              container = null;
    [SerializeField]
    private List<Renderer>                  renderers = null;

    private List<Material>                  allMarkedMaterials = null;

    private Dictionary<Renderer, Material>  normalMaterials = null;
    private Dictionary<Renderer, Material>  markedMaterials = null;    

    private float                           outlineWidth = 0.01f;
    private Shader                          markedShader = null;
    private Coroutine                       hitRoutine = null;
    
    private MaterialsContainer Container
    {
        get
        {
            if (container == null)
            {
                container = GetComponent<MaterialsContainer>();

                if (container == null)
                {
                    container = gameObject.AddComponent<MaterialsContainer>();
                }
            }

            return container;
        }
    }

    public void Init(VehicleController unitBehavior)
    {
        InitRenderers();
        unit = unitBehavior;

        if (Application.isPlaying)
        {
            StartCoroutine(Initializing());
        }
    }

    private void Start()
    {
        GetComponent<IUnitBehaviour>().AddSubscriber(this);
    }

    private IEnumerator HitRoutine(float t)
    {        
        float time = t;
        while (time > 0)
        {
            time -= Time.deltaTime;
            if (time < 0)
            {
                time = 0;
            }
            HitColor = 1 - time / t;
            yield return null;
        }
        time = t;
        while (time > 0)
        {
            time -= Time.deltaTime;
            if (time < 0)
            {
                time = 0;
            }
            HitColor = time / t;
            yield return null;
        }

        HitColor = 0;
    }

    private float HitColor
    {
        get
        {
            return color.g;
        }

        set
        {
            color.b = value;
            color.g = value;

            if (allMarkedMaterials == null)
            {
                return;
            }

            for (int i = 0; i < allMarkedMaterials.Count; i++)
            {
                if (allMarkedMaterials[i] == null)
                {
                    continue;
                }

                allMarkedMaterials[i].SetColor("_OutlineColor", color);
            }
        }
    }

    public void SetMarkedStatus(bool status)
    {
        if (status == marked || renderers == null)
        {
            return;
        }

        Dictionary<Renderer, Material> dictionary = status ? markedMaterials : normalMaterials;

        if (dictionary == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Count; i++)
        {
            Renderer rend = renderers[i];
            Material material = null;
            if (dictionary.TryGetValue(rend, out material))
            {
                rend.material = material;
            }
        }

        marked = status;
    }

    public void InitRenderers()
    {        
        renderers = Container.Renderers;
    }

    private IEnumerator Initializing()
    {
        yield return new WaitForSeconds(0.5f);
        Dispatcher.Subscribe(EventId.ZoomStateChanged, OnZoomStateChanged);

        string vehicleShaderName = renderers[0].sharedMaterial.shader.name;
        string nameShader = vehicleShaderName == normalShader ? outlineShader : outlineShader2;

        markedShader = Shader.Find(nameShader);
        if (markedShader == null)
        {
            Debug.LogError(name + " markedShader with name " + nameShader + " is NULL!!!", this);
            yield break;
        }

        normalMaterials = new Dictionary<Renderer, Material>(renderers.Count);
        markedMaterials = new Dictionary<Renderer, Material>(renderers.Count);
        allMarkedMaterials = new List<Material>();
        outlineWidth = StaticContainer.MainCamera.Zoom ? zoomedOutline : normalOutline;

        for (int i = 0; i < renderers.Count; i++)
        {
            Renderer rend = renderers[i];
            Material material = rend.material;
            normalMaterials[rend] = material;

            Material markedMaterial = new Material(markedShader);
            markedMaterial.CopyPropertiesFromMaterial(material);
            markedMaterial.SetColor("_OutlineColor", color);            

            Material mat = Instantiate(markedMaterial);
            mat.SetFloat("_Outline", outlineWidth);
            Destroy(markedMaterial);
            markedMaterials[rend] = mat;
            allMarkedMaterials.Add(mat);
        }
    }    

    private void SetOutLineWidth(bool inZoom)
    {
        if (markedMaterials == null)
        {
            return;
        }

        outlineWidth = inZoom ? zoomedOutline : normalOutline;

        foreach (var markedMat in markedMaterials.Values)
        {
            markedMat.SetFloat("_Outline", outlineWidth);
        }
    }

    private void OnZoomStateChanged(EventId id, EventInfo ei)
    {
        SetOutLineWidth(StaticContainer.MainCamera.Zoom);
    }

    private void OnDestroy()
    {
        if (allMarkedMaterials != null)
        {
            for (int i = 0; i < allMarkedMaterials.Count; i++)
            {
                Destroy(allMarkedMaterials[i]);
            }
        }

        Dispatcher.Unsubscribe(EventId.ZoomStateChanged, OnZoomStateChanged);
    }
}