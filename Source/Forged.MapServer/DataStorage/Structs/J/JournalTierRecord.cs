using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.J;

public sealed record JournalTierRecord
{
    public uint Id;
    public LocalizedString Name;
    public int PlayerConditionID;
}