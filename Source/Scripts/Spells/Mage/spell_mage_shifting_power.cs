// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[SpellScript(MageSpells.SHIFTING_POWER_DAMAGE_PROC)]
internal class SpellMageShiftingPower : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        var caster = Caster;

        if (caster != null && caster.TryGetAura(MageSpells.SHIFTING_POWER, out var aura))
        {
            //creating a list of all spells in casters spell history
            var spellHistory = caster.SpellHistory;

            // looping over all spells that have cooldowns
            foreach (var spell in spellHistory.SpellsOnCooldown)
                spellHistory.ModifyCooldown(spell, System.TimeSpan.FromMilliseconds(aura.SpellInfo.GetEffect(1).BasePoints));
        }
    }
}