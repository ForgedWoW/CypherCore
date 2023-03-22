﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Spells;

class SkillPerfectItemEntry
{
	// the spell id of the spell required - it's named "specialization" to conform with SkillExtraItemEntry
	public uint RequiredSpecialization;

	// perfection proc chance
	public double PerfectCreateChance;

	// itemid of the resulting perfect item
	public uint PerfectItemType;

	public SkillPerfectItemEntry(uint rS = 0, double pCC = 0f, uint pIT = 0)
	{
		RequiredSpecialization = rS;
		PerfectCreateChance = pCC;
		PerfectItemType = pIT;
	}
}