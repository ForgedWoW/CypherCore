// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;

namespace Scripts.Spells.DeathKnight;

[SpellScript(new uint[]
{
    48263, 48265, 48266
})]
public class spell_dk_presence_SpellScript : SpellScript
{
    public void AfterHit()
    {
        var caster = Caster;

        if (HitUnit)
        {
            var runicPower = caster.GetPower(PowerType.RunicPower);
            var aurEff = caster.GetAuraEffect(58647, 0);

            if (aurEff != null)
                runicPower = MathFunctions.CalculatePct(runicPower, aurEff.Amount);
            else
                runicPower = 0;

            caster.SetPower(PowerType.RunicPower, runicPower);
        }
    }
}