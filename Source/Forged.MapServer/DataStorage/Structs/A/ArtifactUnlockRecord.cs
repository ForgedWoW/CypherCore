namespace Forged.MapServer.DataStorage.Structs.A;

public sealed record ArtifactUnlockRecord
{
    public uint Id;
    public uint PowerID;
    public byte PowerRank;
    public ushort ItemBonusListID;
    public uint PlayerConditionID;
    public uint ArtifactID;
}