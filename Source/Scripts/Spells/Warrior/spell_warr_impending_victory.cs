// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior;

[Script] // 202168 - Impending Victory
internal class SpellWarrImpendingVictory : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        var caster = Caster;
        caster.SpellFactory.CastSpell(caster, WarriorSpells.IMPENDING_VICTORY_HEAL, true);
        caster.RemoveAura(WarriorSpells.VICTORIOUS);
    }
}