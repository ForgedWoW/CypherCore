// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed class ContentTuningRecord
{
    public int ExpansionID;
    public int Flags;
    public uint Id;
    public int MaxLevel;
    public int MaxLevelType;
    public int MinItemLevel;
    public int MinLevel;
    public int MinLevelType;
    public int TargetLevelDelta;
    public int TargetLevelMax;
    public int TargetLevelMaxDelta;
    public int TargetLevelMin;
    public ContentTuningFlag GetFlags()
    {
        return (ContentTuningFlag)Flags;
    }

    public int GetScalingFactionGroup()
    {
        var flags = GetFlags();

        if (flags.HasFlag(ContentTuningFlag.Horde))
            return 5;

        if (flags.HasFlag(ContentTuningFlag.Alliance))
            return 3;

        return 0;
    }
}