// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script] // 59262 Grievous Wound
internal class SpellGenRemoveOnFullHealth : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicDamage));
    }

    private void PeriodicTick(AuraEffect aurEff)
    {
        // if it has only periodic effect, allow 1 tick
        var onlyEffect = SpellInfo.Effects.Count == 1;

        if (onlyEffect && aurEff.GetTickNumber() <= 1)
            return;

        if (Target.IsFullHealth)
        {
            Remove(AuraRemoveMode.EnemySpell);
            PreventDefaultAction();
        }
    }
}