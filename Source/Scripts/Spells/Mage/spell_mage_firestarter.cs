// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[Script] // 11366 - Pyroblast
internal class SpellMageFirestarter : SpellScript, ISpellCalcCritChance
{
    public void CalcCritChance(Unit victim, ref double critChance)
    {
        var aurEff = Caster.GetAuraEffect(MageSpells.Firestarter, 0);

        if (aurEff != null)
            if (victim.HealthPct >= aurEff.Amount)
                critChance = 100.0f;
    }
}