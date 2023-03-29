// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[SpellScript(51723)]
public class spell_rog_fan_of_knives_SpellScript : SpellScript, ISpellOnHit, ISpellAfterHit
{
    private bool _hit;

    public void AfterHit()
    {
        var target = HitUnit;

        if (target.HasAura(51690)) //Killing spree debuff #1
            target.RemoveAura(51690);

        if (target.HasAura(61851)) //Killing spree debuff #2
            target.RemoveAura(61851);
    }


    public override bool Load()
    {
        return true;
    }

    public void OnHit()
    {
        if (!_hit)
        {
            var cp = Caster.GetPower(PowerType.ComboPoints);

            if (cp < Caster.GetMaxPower(PowerType.ComboPoints))
                Caster.SetPower(PowerType.ComboPoints, cp + 1);

            _hit = true;
        }
    }
}