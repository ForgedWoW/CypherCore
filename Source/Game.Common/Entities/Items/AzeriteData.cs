// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.DataStorage;
using Game.Common.DataStorage.Structs.A;
using Game.Entities;
using Game.Common.Entities.Items;

namespace Game.Common.Entities.Items;

public class AzeriteData
{
	public ulong Xp;
	public uint Level;
	public uint KnowledgeLevel;
	public List<uint> AzeriteItemMilestonePowers = new();
	public List<AzeriteEssencePowerRecord> UnlockedAzeriteEssences = new();
	public AzeriteItemSelectedEssencesData[] SelectedAzeriteEssences = new AzeriteItemSelectedEssencesData[4];
}
