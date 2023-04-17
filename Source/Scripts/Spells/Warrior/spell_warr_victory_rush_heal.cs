// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior;

// Victory Rush (heal) - 118779
[SpellScript(118779)]
public class SpellWarrVictoryRushHeal : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var caster = Caster;
        var heal = HitHeal;

        var glyphOfVictoryRush = caster.GetAuraEffect(WarriorSpells.GLYPH_OF_MIGHTY_VICTORY, 0);

        if (glyphOfVictoryRush != null)
            MathFunctions.AddPct(ref heal, glyphOfVictoryRush.Amount);

        HitHeal = heal;
    }
}