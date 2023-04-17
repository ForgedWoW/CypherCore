// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Rogue;

[SpellScript(199804)]
public class SpellRogBetweenTheEyesSpellScript : SpellScript, ISpellAfterHit, ISpellOnTakePower
{
    private int _cp = 0;

    public void AfterHit()
    {
        var target = HitUnit;

        if (target != null)
        {
            var aura = target.GetAura(TrueBearingIDs.BetweenTheEyes, Caster.GUID);

            if (aura != null)
                aura.SetDuration(_cp * Time.IN_MILLISECONDS);
        }
    }

    public void TakePower(SpellPowerCost powerCost)
    {
        if (powerCost.Power == PowerType.ComboPoints)
            _cp = powerCost.Amount;
    }
}