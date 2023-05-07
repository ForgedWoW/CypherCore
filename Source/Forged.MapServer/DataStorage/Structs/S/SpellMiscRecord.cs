// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpellMiscRecord
{
    public uint ActiveIconFileDataID;
    public int ActiveSpellVisualScript;
    public int[] Attributes = new int[15];
    public ushort CastingTimeIndex;
    public uint ContentTuningID;
    public byte DifficultyID;
    public ushort DurationIndex;
    public uint Id;
    public float LaunchDelay;
    public float MinDuration;
    public ushort RangeIndex;
    public byte SchoolMask;
    public int ShowFutureSpellPlayerConditionID;
    public float Speed;
    public uint SpellIconFileDataID;
    public uint SpellID;
    public int SpellVisualScript;
}