using Game.DataStorage;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.S;

public sealed class SpellMiscRecord
{
	public uint Id;
	public int[] Attributes = new int[15];
	public byte DifficultyID;
	public ushort CastingTimeIndex;
	public ushort DurationIndex;
	public ushort RangeIndex;
	public byte SchoolMask;
	public float Speed;
	public float LaunchDelay;
	public float MinDuration;
	public uint SpellIconFileDataID;
	public uint ActiveIconFileDataID;
	public uint ContentTuningID;
	public int ShowFutureSpellPlayerConditionID;
	public int SpellVisualScript;
	public int ActiveSpellVisualScript;
	public uint SpellID;
}
