using System.Collections.Generic;
using UnityEngine;

public class MaterialsContainer : MonoBehaviour
{
    public string ignoreTag = "IgnoreMaterial";

    private List<Material> sharedMaterials;
    private List<Material> processMaterials;
    private Renderer[] renderers;
    private Dictionary<Material, List<Renderer>> renderersBySharedMaterials;

    private List<Material> Materials
    {
        get
        {
            if (processMaterials == null)
                Init();

            return processMaterials;
        }
    }

    private void Init()
    {
        if (renderersBySharedMaterials != null)
            return;

        sharedMaterials = new List<Material>();
        renderers = GetComponentsInChildren<Renderer>(true);
        renderersBySharedMaterials = InitDictionary(renderers);
        processMaterials = SetProcessMaterials(renderersBySharedMaterials, sharedMaterials);
    }

    public List<Material> GetMaterials(Shader shader)
    {
        List<Material> result = new List<Material>();

        if (Materials == null)
            return result;

        for (int i = 0; i < Materials.Count; i++)
        {
            if (Materials[i].shader != shader)
                continue;

            result.Add(Materials[i]);
        }

        return result;
    }

    private List<Material> SetProcessMaterials(Dictionary<Material, List<Renderer>> renderersBySharedMaterials, List<Material> sharedMaterials)
    {
        List<Material> result = new List<Material>();

        for (int i = 0; i < sharedMaterials.Count; i++)
        {
            Material sharedMaterial = sharedMaterials[i];
            Material processMaterial = Instantiate(sharedMaterial);

            result.Add(processMaterial);

            List<Renderer> renderers = renderersBySharedMaterials[sharedMaterial];

            for (int j = 0; j < renderers.Count; j++)
                renderers[j].material = processMaterial;
        }

        return result.Count > 0 ? result : null;
    }

    private Dictionary<Material, List<Renderer>> InitDictionary(Renderer[] renderers)
    {
        Dictionary<Material, List<Renderer>> result = new Dictionary<Material, List<Renderer>>();

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer.CompareTag(ignoreTag))
                continue;

            for (int j = 0; j < renderer.sharedMaterials.Length; j++)
            {
                Material sharedMaterial = renderer.sharedMaterials[j];

                if (sharedMaterial == null)
                    continue;

                if (!sharedMaterials.Contains(sharedMaterial))
                    sharedMaterials.Add(sharedMaterial);

                List<Renderer> renderersBySharedMaterial;

                if (!result.TryGetValue(sharedMaterial, out renderersBySharedMaterial))
                {
                    renderersBySharedMaterial = new List<Renderer> { renderer };
                    result.Add(sharedMaterial, renderersBySharedMaterial);
                }
                else
                {
                    renderersBySharedMaterial.Add(renderer);
                }
            }
        }

        return result.Count > 0 ? result : null;
    }
}