// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Spells;

public struct HitTriggerSpell
{
	public HitTriggerSpell(SpellInfo spellInfo, SpellInfo auraSpellInfo, double procChance)
	{
		TriggeredSpell = spellInfo;
		TriggeredByAura = auraSpellInfo;
		Chance = procChance;
	}

	public SpellInfo TriggeredSpell;

	public SpellInfo TriggeredByAura;

	// ubyte triggeredByEffIdx          This might be needed at a later stage - No need known for now
	public double Chance;
}