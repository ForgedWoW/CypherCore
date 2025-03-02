﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Quest;

[Script] // 76245 - Area Restrict Abom
internal class spell_q13264_q13276_q13288_q13289_area_restrict_abom : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(int effIndex)
	{
		var creature = HitCreature;

		if (creature != null)
		{
			var area = creature.Area;

			if (area != Misc.AreaTheBrokenFront &&
				area != Misc.AreaMordRetharTheDeathGate)
				creature.DespawnOrUnsummon();
		}
	}
}