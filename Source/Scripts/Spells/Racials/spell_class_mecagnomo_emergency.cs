// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Racials;

[SpellScript(312916)]
public class spell_class_mecagnomo_emergency : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();


	public bool CheckProc(ProcEventInfo eventInfo)
	{
		var caster = Caster;

		if (caster.HasAuraState(AuraStateType.Wounded20Percent))
		{
			caster.ModifyAuraState(AuraStateType.Wounded20Percent, false);

			return true;
		}

		return false;
	}


	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		var caster = Caster;

		var triggerOnHealth = caster.CountPctFromMaxHealth(aurEff.Amount);
		var currentHealth = caster.Health;

		// Just falling below threshold
		if (currentHealth > triggerOnHealth && (currentHealth - caster.MaxHealth * 25.0f / 100.0f) <= triggerOnHealth)
			caster.CastSpell(caster, 313010);
	}
}

[SpellScript(313015)]
public class spell_class_mecagnomo_emergency2 : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleHit, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
	}


	private void HandleHit(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		if (!Caster.HasAura(313010))
			PreventDefaultAction();
	}
}

[SpellScript(313010)]
public class spell_class_mecagnomo_emergency3 : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHeal, 0, SpellEffectName.HealPct, SpellScriptHookType.EffectHitTarget));
		SpellEffects.Add(new EffectHandler(HandleHit, 1, SpellEffectName.TriggerSpell, SpellScriptHookType.Launch));
	}

	private void HandleHit(int effIndex)
	{
		if (!Caster.HasAura(313015))
			PreventHitDefaultEffect(effIndex);
	}

	private void HandleHeal(int effIndex)
	{
		var caster = Caster;
		double heal = caster.MaxHealth * 25.0f / 100.0f;
		//caster->SpellHealingBonusDone(caster, GetSpellInfo(), caster->CountPctFromMaxHealth(GetSpellInfo()->GetEffect(effIndex)->BasePoints), DamageEffectType.Heal, GetEffectInfo());
		heal = caster.SpellHealingBonusTaken(caster, SpellInfo, heal, DamageEffectType.Heal);
		HitHeal = (int)heal;
		caster.CastSpell(caster, 313015, true);

		PreventHitDefaultEffect(effIndex);
	}
}