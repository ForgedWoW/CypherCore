namespace Forged.MapServer.DataStorage.Structs.I;

public sealed record ItemLevelSelectorRecord
{
    public uint Id;
    public ushort MinItemLevel;
    public ushort ItemLevelSelectorQualitySetID;
    public ushort AzeriteUnlockMappingSet;
}