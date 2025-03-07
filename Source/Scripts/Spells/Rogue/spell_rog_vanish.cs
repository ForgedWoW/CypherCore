﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[Script] // 1856 - Vanish - VANISH
internal class spell_rog_vanish : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(OnLaunchTarget, 1, SpellEffectName.TriggerSpell, SpellScriptHookType.LaunchTarget));
	}

	private void OnLaunchTarget(int effIndex)
	{
		PreventHitDefaultEffect(effIndex);

		var target = HitUnit;

		target.RemoveAurasByType(AuraType.ModStalked);

		if (!target.IsPlayer)
			return;

		if (target.HasAura(RogueSpells.VanishAura))
			return;

		target.CastSpell(target, RogueSpells.VanishAura, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
		target.CastSpell(target, RogueSpells.StealthShapeshiftAura, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
	}
}