// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[SpellScript(382289)]
public class SpellMageTempestBarrier : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        double absorb = 0;

        if (Caster.TryGetAura(MageSpells.TEMPEST_BARRIER, out var tempestBarrier))
            absorb = MathFunctions.ApplyPct(Caster.Health, tempestBarrier.GetEffect(0).Amount);

        Caster.SpellFactory.CastSpell(Caster, 382290, absorb, false);
    }
}