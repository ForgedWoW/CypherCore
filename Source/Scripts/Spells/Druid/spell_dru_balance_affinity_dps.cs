﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(197488)]
public class spell_dru_balance_affinity_dps : AuraScript, IHasAuraEffects
{


	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	private void LearnSpells(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		Unit caster = GetCaster();
		if (caster == null)
		{
			return;
		}

		Player player = caster.ToPlayer();
		if (player != null)
		{
			player.AddTemporarySpell(ShapeshiftFormSpells.SPELL_DRUID_MOONKIN_FORM);
			player.AddTemporarySpell(BalanceAffinitySpells.SPELL_DRUID_STARSURGE);
			player.AddTemporarySpell(BalanceAffinitySpells.SPELL_DRUID_LUNAR_STRIKE);
			player.AddTemporarySpell(BalanceAffinitySpells.SPELL_DRUID_SOLAR_WRATH);
			player.AddTemporarySpell(BalanceAffinitySpells.SPELL_DRUID_SUNFIRE);
		}
	}

	private void UnlearnSpells(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		Unit caster = GetCaster();
		if (caster == null)
		{
			return;
		}

		Player player = caster.ToPlayer();
		if (player != null)
		{
			player.RemoveTemporarySpell(ShapeshiftFormSpells.SPELL_DRUID_MOONKIN_FORM);
			player.RemoveTemporarySpell(BalanceAffinitySpells.SPELL_DRUID_STARSURGE);
			player.RemoveTemporarySpell(BalanceAffinitySpells.SPELL_DRUID_LUNAR_STRIKE);
			player.RemoveTemporarySpell(BalanceAffinitySpells.SPELL_DRUID_SOLAR_WRATH);
			player.RemoveTemporarySpell(BalanceAffinitySpells.SPELL_DRUID_SUNFIRE);
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(UnlearnSpells, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
		AuraEffects.Add(new AuraEffectApplyHandler(LearnSpells, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
	}
}