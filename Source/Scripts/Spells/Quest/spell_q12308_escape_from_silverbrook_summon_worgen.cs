﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Quest;

[Script] // 48681 - Summon Silverbrook Worgen
internal class spell_q12308_escape_from_silverbrook_summon_worgen : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new DestinationTargetSelectHandler(ModDest, 0, Targets.DestCasterSummon));
	}

	private void ModDest(ref SpellDestination dest)
	{
		float dist  = GetEffectInfo(0).CalcRadius(GetCaster());
		float angle = RandomHelper.FRand(0.75f, 1.25f) * MathFunctions.PI;

		Position pos = GetCaster().GetNearPosition(dist, angle);
		dest.Relocate(pos);
	}
}