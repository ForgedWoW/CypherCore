// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

[SpellScript(1122)]
internal class SpellWarlSummonInfernal : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        var caster = Caster;

        if (caster != null && caster.TryGetAura(WarlockSpells.CRASHING_CHAOS, out var aura))
            for (var i = 0; i < aura.GetEffect(0).BaseAmount; i++)
                caster.AddAura(WarlockSpells.CRASHING_CHAOS_AURA, caster);
    }
}