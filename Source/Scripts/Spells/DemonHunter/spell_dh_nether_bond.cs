// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(207810)]
public class spell_dh_nether_bond : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        var caster = Caster;

        if (caster == null)
            return;

        caster.CastSpell(caster, DemonHunterSpells.NETHER_BOND_PERIODIC, true);
    }
}