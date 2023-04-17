// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(344862)]
public class SpellDhChaosStrikeSpecless : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        var caster = Caster;

        caster.SpellFactory.CastSpell(DemonHunterSpells.CHAOS_STRIKE, true);
    }
}