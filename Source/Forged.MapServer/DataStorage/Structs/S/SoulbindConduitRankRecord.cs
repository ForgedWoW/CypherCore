namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SoulbindConduitRankRecord
{
    public uint Id;
    public int RankIndex;
    public int SpellID;
    public float AuraPointsOverride;
    public uint SoulbindConduitID;
}