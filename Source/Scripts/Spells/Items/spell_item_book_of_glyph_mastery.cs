// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_book_of_glyph_mastery : SpellScript, ISpellCheckCast, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Load()
	{
		return Caster.TypeId == TypeId.Player;
	}

	public SpellCastResult CheckCast()
	{
		if (SkillDiscovery.HasDiscoveredAllSpells(SpellInfo.Id, Caster.AsPlayer))
		{
			SetCustomCastResultMessage(SpellCustomErrors.LearnedEverything);

			return SpellCastResult.CustomError;
		}

		return SpellCastResult.SpellCastOk;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(int effIndex)
	{
		var caster = Caster.AsPlayer;
		var spellId = SpellInfo.Id;

		// learn random explicit discovery recipe (if any)
		var discoveredSpellId = SkillDiscovery.GetExplicitDiscoverySpell(spellId, caster);

		if (discoveredSpellId != 0)
			caster.LearnSpell(discoveredSpellId, false);
	}
}