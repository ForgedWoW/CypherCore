// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[SpellScript(207289)]
public class SpellDkUnholyFrenzy : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 0, AuraType.MeleeSlow, AuraEffectHandleModes.Real));
    }


    private void HandleApply(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var target = Target;
        var caster = Caster;

        if (target == null || caster == null)
            return;

        caster.Events.AddRepeatEventAtOffset(() =>
                                             {
                                                 if (target == null || caster == null)
                                                     return default;

                                                 if (target.HasAura(156004))
                                                     caster.SpellFactory.CastSpell(target, DeathKnightSpells.FESTERING_WOUND_DAMAGE, true);

                                                 if (caster.HasAura(156004))
                                                     return TimeSpan.FromSeconds(2);

                                                 return default;
                                             },
                                             TimeSpan.FromMilliseconds(100));
    }
}