﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman;

// 114911 - Ancestral Guidance Heal
[SpellScript(114911)]
internal class spell_sha_ancestral_guidance_heal : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(ResizeTargets, 0, Targets.UnitDestAreaAlly));
	}

	private void ResizeTargets(List<WorldObject> targets)
	{
		SelectRandomInjuredTargets(targets, 3, true);
	}
}