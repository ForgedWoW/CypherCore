// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.DataStorage;

public sealed class DungeonEncounterRecord
{
	public LocalizedString Name;
	public uint Id;
	public short MapID;
	public int DifficultyID;
	public int OrderIndex;
	public int CompleteWorldStateID;
	public sbyte Bit;
	public int Flags;
	public int SpellIconFileID;
	public int Faction;
}