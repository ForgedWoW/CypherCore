﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior;

//20243 - Devastate
[SpellScript(20243)]
public class spell_warr_devastate : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleOnHit, 2, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleOnHit(int effIndex)
	{
		var caster = Caster;

		if (caster == null)
			return;

		// https://www.wowhead.com/spell=23922/shield-slam
		if (RandomHelper.randChance(Global.SpellMgr.GetSpellInfo(WarriorSpells.DEVASTATE, Difficulty.None).GetEffect(effIndex).BasePoints))
		{
			var player = caster.AsPlayer;

			if (player != null)
				player.SpellHistory.ResetCooldown(WarriorSpells.SHIELD_SLAM, true);
		}
	}
}