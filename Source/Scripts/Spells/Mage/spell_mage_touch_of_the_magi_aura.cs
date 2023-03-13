// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 210824 - Touch of the Magi (Aura)
internal class spell_mage_touch_of_the_magi_aura : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MageSpells.TouchOfTheMagiExplode);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		var damageInfo = eventInfo.DamageInfo;

		if (damageInfo != null)
			if (damageInfo.Attacker == Caster &&
				damageInfo.Victim == Target)
			{
				var extra = MathFunctions.CalculatePct(damageInfo.Damage, 25);

				if (extra > 0)
					aurEff.ChangeAmount(aurEff.Amount + (int)extra);
			}
	}

	private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var amount = aurEff.Amount;

		if (amount == 0 ||
			TargetApplication.RemoveMode != AuraRemoveMode.Expire)
			return;

		var caster = Caster;

		caster?.CastSpell(Target, MageSpells.TouchOfTheMagiExplode, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, amount));
	}
}