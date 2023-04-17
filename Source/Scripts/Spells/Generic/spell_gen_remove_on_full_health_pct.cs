// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script] // 71316 - Glacial Strike
internal class SpellGenRemoveOnFullHealthPct : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 2, AuraType.PeriodicDamagePercent));
    }

    private void PeriodicTick(AuraEffect aurEff)
    {
        // they apply Damage so no need to check for ticks here

        if (Target.IsFullHealth)
        {
            Remove(AuraRemoveMode.EnemySpell);
            PreventDefaultAction();
        }
    }
}