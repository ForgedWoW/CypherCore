// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.DemonHunter;

[Script]
public class at_shattered_soul_fragment : AreaTriggerAI
{
	public at_shattered_soul_fragment(AreaTrigger areatrigger) : base(areatrigger) { }

	public override void OnUnitEnter(Unit unit)
	{
		if (unit != At.GetCaster() || !unit.IsPlayer || unit.AsPlayer.Class != Class.DemonHunter)
			return;

		switch (At.Entry)
		{
			case 10665:
				if (At.GetCaster().AsPlayer.GetPrimarySpecialization() == TalentSpecialization.DemonHunterHavoc)
					At.GetCaster().CastSpell(At.GetCaster(), ShatteredSoulsSpells.SOUL_FRAGMENT_HEAL_25_HAVOC, true);

				At.Remove();

				break;

			case 10666:
				if (At.GetCaster().AsPlayer.GetPrimarySpecialization() == TalentSpecialization.DemonHunterHavoc)
					At.GetCaster().CastSpell(At.GetCaster(), ShatteredSoulsSpells.SOUL_FRAGMENT_HEAL_25_HAVOC, true);

				At.Remove();

				break;
		}
	}
}