// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.J;

public sealed class JournalEncounterRecord
{
	public LocalizedString Name;
	public LocalizedString Description;
	public Vector2 Map;
	public uint Id;
	public ushort JournalInstanceID;
	public ushort DungeonEncounterID;
	public uint OrderIndex;
	public ushort FirstSectionID;
	public ushort UiMapID;
	public uint MapDisplayConditionID;
	public int Flags;
	public sbyte DifficultyMask;
}