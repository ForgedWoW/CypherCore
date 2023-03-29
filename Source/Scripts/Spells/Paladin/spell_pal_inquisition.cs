﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Paladin;

// 84963  - Inquisition
[SpellScript(84963)]
public class spell_pal_inquisition : SpellScript, ISpellOnTakePower, ISpellAfterHit
{
    private double m_powerTaken = 0.0f;

    public void AfterHit()
    {
        var aura = Caster.GetAura(SpellInfo.Id);

        if (aura != null)
            aura.SetDuration((int)(aura.Duration * m_powerTaken));
    }

    public void TakePower(SpellPowerCost powerCost)
    {
        m_powerTaken = powerCost.Amount;
    }
}