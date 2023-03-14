// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior;

// Victory Rush (heal) - 118779
[SpellScript(118779)]
public class spell_warr_victory_rush_heal : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var caster = Caster;
		var heal = HitHeal;

		var GlyphOfVictoryRush = caster.GetAuraEffect(WarriorSpells.GLYPH_OF_MIGHTY_VICTORY, 0);

		if (GlyphOfVictoryRush != null)
			MathFunctions.AddPct(ref heal, GlyphOfVictoryRush.Amount);

		HitHeal = heal;
	}
}