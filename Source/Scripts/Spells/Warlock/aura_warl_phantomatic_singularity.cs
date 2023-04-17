// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// 205179
[SpellScript(205179)]
public class AuraWarlPhantomaticSingularity : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public void OnTick(AuraEffect unnamedParameter)
    {
        var caster = Caster;

        if (Caster)
            caster.SpellFactory.CastSpell(Target.Location, WarlockSpells.PHANTOMATIC_SINGULARITY_DAMAGE, true);
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 0, AuraType.PeriodicLeech));
    }
}