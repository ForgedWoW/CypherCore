namespace Forged.MapServer.DataStorage.Structs.A;

public sealed record ArtifactTierRecord
{
    public uint Id;
    public uint ArtifactTier;
    public uint MaxNumTraits;
    public uint MaxArtifactKnowledge;
    public uint KnowledgePlayerCondition;
    public uint MinimumEmpowerKnowledge;
}