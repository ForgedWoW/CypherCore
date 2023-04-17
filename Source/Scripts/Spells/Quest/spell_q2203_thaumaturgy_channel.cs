// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Quest;

[Script] // 9712 - Thaumaturgy Channel
internal class SpellQ2203ThaumaturgyChannel : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicTriggerSpell));
    }

    private void HandleEffectPeriodic(AuraEffect aurEff)
    {
        PreventDefaultAction();
        var caster = Caster;

        if (caster)
            caster.SpellFactory.CastSpell(caster, QuestSpellIds.THAUMATURGY_CHANNEL, false);
    }
}