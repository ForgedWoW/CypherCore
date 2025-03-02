﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script("spell_future_you_whisper_to_controller_random", 2u)]
[Script("spell_wyrmrest_defender_whisper_to_controller_random", 1u)]
[Script("spell_past_you_whisper_to_controller_random", 2u)]
internal class spell_gen_whisper_to_controller_random : SpellScript, IHasSpellEffects
{
	private readonly uint _text;

	public List<ISpellEffect> SpellEffects { get; } = new();

	public spell_gen_whisper_to_controller_random(uint text)
	{
		_text = text;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(int effIndex)
	{
		// Same for all spells
		if (!RandomHelper.randChance(20))
			return;

		var target = HitCreature;

		if (target != null)
		{
			var targetSummon = target.ToTempSummon();

			if (targetSummon != null)
			{
				var player = targetSummon.GetSummonerUnit().AsPlayer;

				if (player != null)
					targetSummon.AI.Talk(_text, player);
			}
		}
	}
}