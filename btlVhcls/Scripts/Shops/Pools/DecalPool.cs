public class DecalPool : BodykitPool<Decal>
{
    protected override Decal[] GetItems()
    {
        HelpTools.ImportComponentsBodykitInEditor(Instance.ReferencedBodykits, GameData.vehiclesDataStorage, "decals");
        return base.GetItems();
    }
}
