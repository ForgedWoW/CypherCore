﻿using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_whisper_to_controller : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return CliDB.BroadcastTextStorage.HasRecord((uint)spellInfo.GetEffect(0).CalcValue());
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHit));
	}

	private void HandleScript(uint effIndex)
	{
		TempSummon casterSummon = GetCaster().ToTempSummon();

		if (casterSummon != null)
		{
			Player target = casterSummon.GetSummonerUnit().ToPlayer();

			if (target != null)
				casterSummon.Whisper((uint)GetEffectValue(), target, false);
		}
	}
}