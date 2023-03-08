// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Paladin;

// 26573 - Consecration
[Script] //  9228 - AreaTriggerId
internal class areatrigger_pal_consecration : AreaTriggerAI
{
	public areatrigger_pal_consecration(AreaTrigger areatrigger) : base(areatrigger) { }

	public override void OnUnitEnter(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster != null)
		{
			// 243597 is also being cast as protection, but CreateObject is not sent, either serverside areatrigger for this aura or unused - also no visual is seen
			if (unit == caster &&
				caster.IsPlayer &&
				caster.				AsPlayer.GetPrimarySpecialization() == TalentSpecialization.PaladinProtection)
				caster.CastSpell(caster, PaladinSpells.ConsecrationProtectionAura);

			if (caster.IsValidAttackTarget(unit))
				if (caster.HasAura(PaladinSpells.ConsecratedGroundPassive))
					caster.CastSpell(unit, PaladinSpells.ConsecratedGroundSlow);
		}
	}

	public override void OnUnitExit(Unit unit)
	{
		if (at.CasterGuid == unit.GUID)
			unit.RemoveAurasDueToSpell(PaladinSpells.ConsecrationProtectionAura, at.CasterGuid);

		unit.RemoveAurasDueToSpell(PaladinSpells.ConsecratedGroundSlow, at.CasterGuid);
	}
}