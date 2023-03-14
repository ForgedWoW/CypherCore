// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.DemonHunter;

[SpellScript(209400)]
public class spell_dh_razor_spikes : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		var damageInfo = eventInfo.DamageInfo;

		if (damageInfo == null)
			return false;

		if (damageInfo.AttackType == WeaponAttackType.BaseAttack || damageInfo.AttackType == WeaponAttackType.OffAttack)
		{
			var caster = damageInfo.Attacker;
			var target = damageInfo.Victim;

			if (caster == null || target == null || !caster.AsPlayer)
				return false;

			if (!caster.IsValidAttackTarget(target))
				return false;

			if (caster.HasAura(DemonHunterSpells.DEMON_SPIKES_BUFF))
				caster.Events.AddEventAtOffset(() => { caster.CastSpell(target, DemonHunterSpells.RAZOR_SPIKES_SLOW, true); }, TimeSpan.FromMilliseconds(750));

			return true;
		}

		return false;
	}
}