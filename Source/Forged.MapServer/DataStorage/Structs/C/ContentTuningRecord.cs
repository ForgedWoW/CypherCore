using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed record ContentTuningRecord
{
    public uint Id;
    public int Flags;
    public int ExpansionID;
    public int MinLevel;
    public int MaxLevel;
    public int MinLevelType;
    public int MaxLevelType;
    public int TargetLevelDelta;
    public int TargetLevelMaxDelta;
    public int TargetLevelMin;
    public int TargetLevelMax;
    public int MinItemLevel;
    public float QuestXpMultiplier;

    public ContentTuningFlag GetFlags() { return (ContentTuningFlag)Flags; }

    public int GetScalingFactionGroup()
    {
        ContentTuningFlag flags = GetFlags();
        if (flags.HasFlag(ContentTuningFlag.Horde))
            return 5;

        if (flags.HasFlag(ContentTuningFlag.Alliance))
            return 3;

        return 0;
    }
}