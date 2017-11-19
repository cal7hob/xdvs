using System;
using System.Collections.Generic;
using UnityEngine;

public class PatternPool : BodykitPool<Pattern>
{
    private List<Pattern> patternItems = new List<Pattern>();
    protected override Pattern[] GetItems()
    {
        HelpTools.ImportComponentsBodykitInEditor(Instance.ReferencedBodykits, GameData.vehiclesDataStorage, "patterns");

        Array.Sort(
            bodykitsInEditor,
            (first, second) => first.position > second.position
                ? 1
                : first.position == second.position ? 0 : -1);

        patternItems.Clear();

        foreach (BodykitInEditor bodykitInEditor in bodykitsInEditor)
            if (bodykitInEditor.gameObject.activeSelf && bodykitInEditor.isLoadedFromServer)
                patternItems.Add(new Pattern((PatternInEditor)bodykitInEditor));

        foreach (Pattern patternItem in patternItems)
            Resources.UnloadAsset(patternItem.textureMask);

        return patternItems.ToArray();
    }

    void OnDestroy()
    {
        foreach (var pattern in patternItems)
        {
            pattern.Dispose();
        }
    }
}
