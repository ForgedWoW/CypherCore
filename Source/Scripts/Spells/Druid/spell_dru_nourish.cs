// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;

namespace Scripts.Spells.Druid;

[SpellScript(50464)]
public class SpellDruNourish : SpellScript
{
    private const int NourishPassive = 203374;
    private const int Rejuvenation = 774;

    public void OnHit()
    {
        var caster = Caster;

        if (caster != null)
        {
            var target = HitUnit;

            if (target != null)
                if (caster.HasAura(NourishPassive))
                    caster.SpellFactory.CastSpell(target, Rejuvenation, true);
        }
    }
}