using System;
using System.Collections.Generic;
using UnityEngine;

public class PatternPool : BodykitPool<Pattern>
{
    [Tooltip("Если галочка убрана – используются ассеты из ресурсов.")]
    public bool legacy = true;

    private PatternEntity[] patternEntities;

    protected override Pattern[] GetItems()
    {
        return legacy ? GetItemsLegacy() : GetItemsNew();
    }

    private Pattern[] GetItemsNew()
    {
        patternEntities = Resources.LoadAll<PatternEntity>(GameManager.CurrentResourcesFolder + "/Entities/Camouflages/");

        HelpTools.ImportComponentsPatternEntity(patternEntities, GameData.vehiclesDataStorage, "patterns");

        Array.Sort(
            patternEntities,
            (first, second) => first.position > second.position
                ? 1
                : first.position == second.position ? 0 : -1);

        List<Pattern> patternItems = new List<Pattern>();

        foreach (PatternEntity patternEntity in patternEntities)
        {
            if (patternEntity.isLoadedFromServer)
                patternItems.Add(new Pattern(patternEntity));
        }

        foreach (Pattern patternItem in patternItems)
            Resources.UnloadAsset(patternItem.textureMask);

        return patternItems.ToArray();
    }

    private Pattern[] GetItemsLegacy()
    {
        HelpTools.ImportComponentsBodykitInEditor(Instance.ReferencedBodykits, GameData.vehiclesDataStorage, "patterns");

        Array.Sort(
            bodykitsInEditor,
            (first, second) => first.position > second.position
                ? 1
                : first.position == second.position ? 0 : -1);

        List<Pattern> patternItems = new List<Pattern>();

        foreach (BodykitInEditor bodykitInEditor in bodykitsInEditor)
        {
            if (bodykitInEditor.gameObject.activeSelf && bodykitInEditor.isLoadedFromServer)
                patternItems.Add(new Pattern((PatternInEditor)bodykitInEditor));
        }

        foreach (BodykitInEditor bodykitInEditor in bodykitsInEditor)
            ((PatternInEditor)bodykitInEditor).UnloadTexture();

        GC.Collect();

        return patternItems.ToArray();
    }
}
