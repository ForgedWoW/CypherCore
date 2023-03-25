// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Spells.Skills;

class SkillExtraItemEntry
{
	// the spell id of the specialization required to create extra items
	public uint RequiredSpecialization;

	// the chance to create one additional item
	public double AdditionalCreateChance;

	// maximum number of extra items created per crafting
	public byte AdditionalMaxNum;

	public SkillExtraItemEntry(uint rS = 0, double aCC = 0f, byte aMN = 0)
	{
		RequiredSpecialization = rS;
		AdditionalCreateChance = aCC;
		AdditionalMaxNum = aMN;
	}
}