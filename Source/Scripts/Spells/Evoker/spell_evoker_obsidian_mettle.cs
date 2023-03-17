// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.OBSIDIAN_SCALES)]
public class spell_evoker_obsidian_mettle : SpellScript, ISpellBeforeCast
{
	public void BeforeCast()
	{
		if (Caster.TryGetAsPlayer(out var player) && player.HasSpell(EvokerSpells.OBSIDIAN_METTLE))
		{
            Spell.SetSpellValue(Framework.Constants.SpellValueMod.BasePoint3, 0f);
        }
		else
        {
            Spell.SetSpellValue(Framework.Constants.SpellValueMod.BasePoint1, 0f);
            Spell.SetSpellValue(Framework.Constants.SpellValueMod.BasePoint2, 0f);
        }
	}
}