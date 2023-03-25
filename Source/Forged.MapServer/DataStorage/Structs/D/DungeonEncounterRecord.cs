// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.D;

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