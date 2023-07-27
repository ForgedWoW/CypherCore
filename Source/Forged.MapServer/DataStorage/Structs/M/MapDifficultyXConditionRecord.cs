using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.M;

public sealed record MapDifficultyXConditionRecord
{
    public uint Id;
    public LocalizedString FailureDescription;
    public uint PlayerConditionID;
    public int OrderIndex;
    public uint MapDifficultyID;
}