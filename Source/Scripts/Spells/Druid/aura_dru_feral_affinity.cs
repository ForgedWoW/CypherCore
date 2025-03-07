﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(202157)]
public class aura_dru_feral_affinity : AuraScript, IHasAuraEffects
{
	private readonly List<uint> LearnedSpells = new()
	{
		(uint)DruidSpells.FELINE_SWIFTNESS,
		(uint)DruidSpells.SHRED,
		(uint)DruidSpells.RAKE,
		(uint)DruidSpells.RIP,
		(uint)DruidSpells.FEROCIOUS_BITE,
		(uint)DruidSpells.SWIPE_CAT
	};

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(AfterApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
		AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void AfterApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var target = Target.AsPlayer;

		if (target != null)
			foreach (var spellId in LearnedSpells)
				target.LearnSpell(spellId, false);
	}

	private void AfterRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var target = Target.AsPlayer;

		if (target != null)
			foreach (var spellId in LearnedSpells)
				target.RemoveSpell(spellId);
	}
}