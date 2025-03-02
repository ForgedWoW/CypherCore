﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_two_forms : SpellScript, ISpellCheckCast, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public SpellCastResult CheckCast()
	{
		if (Caster.IsInCombat)
		{
			SetCustomCastResultMessage(SpellCustomErrors.CantTransform);

			return SpellCastResult.CustomError;
		}

		// Player cannot transform to human form if he is forced to be worgen for some reason (Darkflight)
		if (Caster.GetAuraEffectsByType(AuraType.WorgenAlteredForm).Count > 1)
		{
			SetCustomCastResultMessage(SpellCustomErrors.CantTransform);

			return SpellCastResult.CustomError;
		}

		return SpellCastResult.SpellCastOk;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleTransform, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleTransform(int effIndex)
	{
		var target = HitUnit;
		PreventHitDefaultEffect(effIndex);

		if (target.HasAuraType(AuraType.WorgenAlteredForm))
			target.RemoveAurasByType(AuraType.WorgenAlteredForm);
		else // Basepoints 1 for this aura control whether to trigger transform transition animation or not.
			target.CastSpell(target, GenericSpellIds.AlteredForm, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, 1));
	}
}