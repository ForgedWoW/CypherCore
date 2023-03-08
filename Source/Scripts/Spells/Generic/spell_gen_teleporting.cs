﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_teleporting : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(int effIndex)
	{
		var target = HitUnit;

		if (!target.IsPlayer)
			return;

		// return from top
		if (target.ToPlayer().GetAreaId() == Misc.AreaVioletCitadelSpire)
			target.CastSpell(target, GenericSpellIds.TeleportSpireDown, true);
		// teleport atop
		else
			target.CastSpell(target, GenericSpellIds.TeleportSpireUp, true);
	}
}