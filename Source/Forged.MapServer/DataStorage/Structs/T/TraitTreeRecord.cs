using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.T;

public sealed record TraitTreeRecord
{
    public uint Id;
    public int TraitSystemID;
    public int Unused1000_1;
    public int FirstTraitNodeID;
    public int PlayerConditionID;
    public int Flags;
    public float Unused1000_2;
    public float Unused1000_3;

    public TraitTreeFlag GetFlags() { return (TraitTreeFlag)Flags; }
}