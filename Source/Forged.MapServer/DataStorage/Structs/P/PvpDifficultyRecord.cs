using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.P;

public sealed record PvpDifficultyRecord
{
    public uint Id;
    public byte RangeIndex;
    public byte MinLevel;
    public byte MaxLevel;
    public uint MapID;

    // helpers

    public BattlegroundBracketId GetBracketId() => (BattlegroundBracketId)RangeIndex;
}