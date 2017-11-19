using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class PurchasedDecal : PurchasedPattern 
{
    public PurchasedDecal(int camoId, double camoLifetime)
        : base(camoId, camoLifetime) { }
}

public static class PurchasedDecalExtension
{
    public static bool TryToTakeAwayById(this List<PurchasedDecal> source, int decalId, out int nextDecalId)
    {
        PurchasedDecal targetDecal = source.FirstOrDefault(decal => decal.id == decalId) ?? source.First();

        if (targetDecal.IsDead())
        {
            source.Remove(targetDecal);

            for (int i = source.Count-1; i >= 0; --i) {
                if (source[i].IsDead()) {
                    source.RemoveAt(i);
                }
            }

            nextDecalId = source.Any() ? source.First().id : 0;

            return true;
        }

        nextDecalId = targetDecal.id;

        return false;
    }
}