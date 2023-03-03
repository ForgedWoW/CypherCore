﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
    // 12328, 18765, 35429 - Sweeping Strikes
    [Script]
	internal class spell_warr_sweeping_strikes : AuraScript, IAuraCheckProc, IHasAuraEffects
	{
		private Unit _procTarget;

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(WarriorSpells.SWEEPING_STRIKES_EXTRA_ATTACK_1, WarriorSpells.SWEEPING_STRIKES_EXTRA_ATTACK_2);
		}

		public bool CheckProc(ProcEventInfo eventInfo)
		{
			_procTarget = eventInfo.GetActor().SelectNearbyTarget(eventInfo.GetProcTarget());

			return _procTarget;
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		}

		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
		{
			PreventDefaultAction();
			var damageInfo = eventInfo.GetDamageInfo();

			if (damageInfo != null)
			{
				var spellInfo = damageInfo.GetSpellInfo();

				if (spellInfo != null &&
				    (spellInfo.Id == WarriorSpells.BLADESTORM_PERIODIC_WHIRLWIND || (spellInfo.Id == WarriorSpells.EXECUTE && !_procTarget.HasAuraState(AuraStateType.Wounded20Percent))))
				{
					// If triggered by Execute (while Target is not under 20% hp) or Bladestorm deals normalized weapon Damage
					GetTarget().CastSpell(_procTarget, WarriorSpells.SWEEPING_STRIKES_EXTRA_ATTACK_2, new CastSpellExtraArgs(aurEff));
				}
				else
				{
					CastSpellExtraArgs args = new(aurEff);
					args.AddSpellMod(SpellValueMod.BasePoint0, (int)damageInfo.GetDamage());
					GetTarget().CastSpell(_procTarget, WarriorSpells.SWEEPING_STRIKES_EXTRA_ATTACK_1, args);
				}
			}
		}
	}
}