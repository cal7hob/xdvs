using System;
using System.Collections.Generic;

public class BodykitPool<TBodykit> : ShopItemPool<TBodykit>
    where TBodykit : Bodykit
{
    public BodykitInEditor[] bodykitsInEditor;

    public override BodykitInEditor[] ReferencedBodykits
    {
        get { return bodykitsInEditor; }
    }

    protected override TBodykit[] GetItems()
    {
        Array.Sort(
            array:      bodykitsInEditor,
            comparison: (first, second) => first.position > second.position
                            ? 1
                            : first.position == second.position ? 0 : -1);

        List<TBodykit> bodyKitItems = new List<TBodykit>();

        foreach (BodykitInEditor bodykitInEditor in bodykitsInEditor)
            if (bodykitInEditor.gameObject.activeSelf)
                bodyKitItems.Add(
                    (TBodykit)Activator.CreateInstance(
                        typeof(TBodykit),
                        new object[] { bodykitInEditor }));

        return bodyKitItems.ToArray();
    }
}
