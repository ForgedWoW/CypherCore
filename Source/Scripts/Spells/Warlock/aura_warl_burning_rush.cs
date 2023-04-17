// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// Burning Rush - 111400
[SpellScript(111400)]
public class AuraWarlBurningRush : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 1, AuraType.PeriodicDamagePercent));
    }

    private void OnTick(AuraEffect unnamedParameter)
    {
        if (Caster)
        {
            // This way if the current tick takes you below 4%, next tick won't execute
            var basepoints = Caster.CountPctFromMaxHealth(4);

            if (Caster.Health <= basepoints || Caster.Health - basepoints <= basepoints)
                Aura.SetDuration(0);
        }
    }
}