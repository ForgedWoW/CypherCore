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
internal class spell_gen_profession_research : SpellScript, ISpellCheckCast, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Load()
	{
		return Caster.IsTypeId(TypeId.Player);
	}

	public SpellCastResult CheckCast()
	{
		var player = Caster.AsPlayer;

		if (SkillDiscovery.HasDiscoveredAllSpells(SpellInfo.Id, player))
		{
			SetCustomCastResultMessage(SpellCustomErrors.NothingToDiscover);

			return SpellCastResult.CustomError;
		}

		return SpellCastResult.SpellCastOk;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 1, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(int effIndex)
	{
		var caster = Caster.AsPlayer;
		var spellId = SpellInfo.Id;

		// Learn random explicit discovery recipe (if any)
		// Players will now learn 3 recipes the very first Time they perform Northrend Inscription Research (3.3.0 patch notes)
		if (spellId == GenericSpellIds.NorthrendInscriptionResearch &&
			!SkillDiscovery.HasDiscoveredAnySpell(spellId, caster))
			for (var i = 0; i < 2; ++i)
			{
				var _discoveredSpellId = SkillDiscovery.GetExplicitDiscoverySpell(spellId, caster);

				if (_discoveredSpellId != 0)
					caster.LearnSpell(_discoveredSpellId, false);
			}

		var discoveredSpellId = SkillDiscovery.GetExplicitDiscoverySpell(spellId, caster);

		if (discoveredSpellId != 0)
			caster.LearnSpell(discoveredSpellId, false);
	}
}