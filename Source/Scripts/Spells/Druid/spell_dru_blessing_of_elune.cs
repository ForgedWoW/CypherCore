// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(new uint[]
{
    190984, 194153
})]
public class SpellDruBlessingOfElune : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var caster = Caster;

        if (caster == null)
            return;

        var power = HitDamage;

        var aura = caster.GetAura(202737);

        if (aura != null)
        {
            var aurEff = aura.GetEffect(0);

            if (aurEff != null)
                power += MathFunctions.CalculatePct(power, aurEff.Amount);
        }

        HitDamage = power;
    }
}