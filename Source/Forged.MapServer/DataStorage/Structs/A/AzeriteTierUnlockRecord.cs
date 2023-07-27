namespace Forged.MapServer.DataStorage.Structs.A;

public sealed record AzeriteTierUnlockRecord
{
    public uint Id;
    public byte ItemCreationContext;
    public byte Tier;
    public byte AzeriteLevel;
    public uint AzeriteTierUnlockSetID;
}