namespace Forged.MapServer.DataStorage.Structs.T;

public sealed record TraitTreeLoadoutEntryRecord
{
    public uint Id;
    public int TraitTreeLoadoutID;
    public int SelectedTraitNodeID;
    public int SelectedTraitNodeEntryID;
    public int NumPoints;
    public int OrderIndex;
}