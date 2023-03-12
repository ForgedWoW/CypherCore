// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.DataStorage;

namespace Game.Entities;

public class AzeriteData
{
	public ulong Xp;
	public uint Level;
	public uint KnowledgeLevel;
	public List<uint> AzeriteItemMilestonePowers = new();
	public List<AzeriteEssencePowerRecord> UnlockedAzeriteEssences = new();
	public AzeriteItemSelectedEssencesData[] SelectedAzeriteEssences = new AzeriteItemSelectedEssencesData[4];
}