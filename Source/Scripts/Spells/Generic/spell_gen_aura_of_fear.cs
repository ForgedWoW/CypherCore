// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script] // 28313 - Aura of Fear
internal class SpellGenAuraOfFear : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicTriggerSpell));
    }

    private void PeriodicTick(AuraEffect aurEff)
    {
        PreventDefaultAction();

        if (!RandomHelper.randChance(SpellInfo.ProcChance))
            return;

        Target.SpellFactory.CastSpell(null, aurEff.SpellEffectInfo.TriggerSpell, true);
    }
}