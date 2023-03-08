// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Priest;

[SpellScript(32546)]
public class spell_pri_binding_heal : SpellScript, ISpellAfterCast
{
	public void AfterCast()
	{
		var caster = Caster;

		if (caster == null)
			return;

		if (caster.SpellHistory.HasCooldown(PriestSpells.HOLY_WORD_SANCTIFY))
			caster.			SpellHistory.ModifyCooldown(PriestSpells.HOLY_WORD_SANCTIFY, TimeSpan.FromSeconds(-3 * Time.InMilliseconds));

		if (caster.SpellHistory.HasCooldown(PriestSpells.HOLY_WORD_SERENITY))
			caster.			SpellHistory.ModifyCooldown(PriestSpells.HOLY_WORD_SERENITY, TimeSpan.FromSeconds(-3 * Time.InMilliseconds));
	}
}