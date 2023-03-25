// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SpellXSpellVisualRecord
{
	public uint Id;
	public byte DifficultyID;
	public uint SpellVisualID;
	public float Probability;
	public int Flags;
	public int Priority;
	public int SpellIconFileID;
	public int ActiveIconFileID;
	public ushort ViewerUnitConditionID;
	public uint ViewerPlayerConditionID;
	public ushort CasterUnitConditionID;
	public uint CasterPlayerConditionID;
	public uint SpellID;
}