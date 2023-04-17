// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[SpellScript(273980)]
public class AuraGripOfTheDead : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 0, AuraType.PeriodicDummy));
    }


    private void OnTick(AuraEffect unnamedParameter)
    {
        var target = Target;

        if (target != null)
        {
            var caster = Caster;

            if (caster != null)
                caster.SpellFactory.CastSpell(target, DeathKnightSpells.GRIP_OF_THE_DEAD_SLOW, true);
        }
    }
}