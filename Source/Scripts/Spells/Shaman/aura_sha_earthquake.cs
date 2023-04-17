// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// 61882
[SpellScript(61882)]
public class AuraShaEarthquake : AuraScript
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 1, AuraType.PeriodicDummy));
    }

    private void HandlePeriodic(AuraEffect unnamedParameter)
    {
        var at = Target.GetAreaTrigger(ShamanSpells.EARTHQUAKE);

        if (at != null)
            Target.SpellFactory.CastSpell(at.Location, ShamanSpells.EARTHQUAKE_TICK, true);
    }
}