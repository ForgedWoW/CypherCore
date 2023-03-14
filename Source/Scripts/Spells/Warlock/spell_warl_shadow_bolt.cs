// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

[SpellScript(686)] // 686 - Shadow Bolt
internal class spell_warl_shadow_bolt : SpellScript, ISpellAfterCast
{
	public void AfterCast()
	{
		Caster.CastSpell(Caster, WarlockSpells.SHADOW_BOLT_SHOULSHARD, true);
	}
}