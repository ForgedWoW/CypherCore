// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;

namespace Scripts.Spells.Paladin;

// 84963  - Inquisition
[SpellScript(84963)]
public class SpellPalInquisition : SpellScript, ISpellOnTakePower, ISpellAfterHit
{
    private double _mPowerTaken = 0.0f;

    public void AfterHit()
    {
        var aura = Caster.GetAura(SpellInfo.Id);

        if (aura != null)
            aura.SetDuration((int)(aura.Duration * _mPowerTaken));
    }

    public void TakePower(SpellPowerCost powerCost)
    {
        _mPowerTaken = powerCost.Amount;
    }
}