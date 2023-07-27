using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.T;

public sealed record TraitNodeEntryRecord
{
    public uint Id;
    public int TraitDefinitionID;
    public int MaxRanks;
    public byte NodeEntryType;

    public TraitNodeEntryType GetNodeEntryType() { return (TraitNodeEntryType)NodeEntryType; }
}