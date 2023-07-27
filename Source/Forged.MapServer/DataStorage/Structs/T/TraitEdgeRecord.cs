namespace Forged.MapServer.DataStorage.Structs.T;

public sealed record TraitEdgeRecord
{
    public uint Id;
    public int VisualStyle;
    public int LeftTraitNodeID;
    public int RightTraitNodeID;
    public int Type;
}