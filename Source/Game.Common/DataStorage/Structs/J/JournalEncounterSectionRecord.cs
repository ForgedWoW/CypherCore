// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.DataStorage.ClientReader;
using Game.DataStorage;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.J;

public sealed class JournalEncounterSectionRecord
{
	public uint Id;
	public LocalizedString Title;
	public LocalizedString BodyText;
	public ushort JournalEncounterID;
	public byte OrderIndex;
	public ushort ParentSectionID;
	public ushort FirstChildSectionID;
	public ushort NextSiblingSectionID;
	public byte Type;
	public uint IconCreatureDisplayInfoID;
	public int UiModelSceneID;
	public int SpellID;
	public int IconFileDataID;
	public int Flags;
	public int IconFlags;
	public sbyte DifficultyMask;
}
