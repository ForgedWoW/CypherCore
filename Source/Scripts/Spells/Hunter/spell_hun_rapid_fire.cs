// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Hunter;

[SpellScript(257044)]
public class SpellHunRapidFire : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 1, AuraType.PeriodicDummy));
    }

    private void OnTick(AuraEffect aurEff)
    {
        var target = Target;

        if (target != null)
            if (Caster)
            {
                Caster.SpellFactory.CastSpell(target, HunterSpells.RAPID_FIRE_MISSILE, true);

                if (Caster.GetPowerPct(PowerType.Focus) != 100)
                    Caster.ModifyPower(PowerType.Focus, +1);
            }
    }
}