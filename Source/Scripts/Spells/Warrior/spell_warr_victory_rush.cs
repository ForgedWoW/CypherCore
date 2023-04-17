// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior;

[Script] // 34428 - Victory Rush
internal class SpellWarrVictoryRush : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        var caster = Caster;

        caster.SpellFactory.CastSpell(caster, WarriorSpells.VICTORY_RUSH_HEAL, true);
        caster.RemoveAura(WarriorSpells.VICTORIOUS);
    }
}