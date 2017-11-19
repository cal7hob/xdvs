using System;
using System.Collections.Generic;
using UnityEngine;

public class PatternPool : BodykitPool<Pattern>
{
    protected override Pattern[] GetItems()
    {
        HelpTools.ImportComponentsBodykitInEditor(Instance.ReferencedBodykits, GameData.vehiclesDataStorage, "patterns");

        Array.Sort(
            bodykitsInEditor,
            (first, second) => first.position > second.position
                ? 1
                : first.position == second.position ? 0 : -1);

        List<Pattern> patternItems = new List<Pattern>();

        foreach (BodykitInEditor bodykitInEditor in bodykitsInEditor)
            if (bodykitInEditor.gameObject.activeSelf)
                patternItems.Add(new Pattern((PatternInEditor)bodykitInEditor));

        foreach (Pattern patternItem in patternItems)
            Resources.UnloadAsset(patternItem.textureMask);

        return patternItems.ToArray();
    }
}
