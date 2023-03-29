// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[SpellScript(382289)]
public class spell_mage_tempest_barrier : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        double absorb = 0;

        if (Caster.TryGetAura(MageSpells.TEMPEST_BARRIER, out var tempestBarrier))
            absorb = MathFunctions.ApplyPct(Caster.Health, tempestBarrier.GetEffect(0).Amount);

        Caster.CastSpell(Caster, 382290, absorb, false);
    }
}