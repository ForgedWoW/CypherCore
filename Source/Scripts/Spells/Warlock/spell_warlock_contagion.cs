// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// 233494 - Contagion
[SpellScript(233494)]
public class SpellWarlockContagion : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.ModSchoolMaskDamageFromCaster));
    }

    private void PeriodicTick(AuraEffect unnamedParameter)
    {
        var caster = Caster;
        var target = Target;

        if (caster == null || target == null)
            return;

        var uaspells = new List<uint>()
        {
            WarlockSpells.UNSTABLE_AFFLICTION_DOT5,
            WarlockSpells.UNSTABLE_AFFLICTION_DOT4,
            WarlockSpells.UNSTABLE_AFFLICTION_DOT3,
            WarlockSpells.UNSTABLE_AFFLICTION_DOT2,
            WarlockSpells.UNSTABLE_AFFLICTION_DOT1
        };

        var hasUa = false;

        foreach (var ua in uaspells)
            if (target.HasAura(ua, caster.GUID))
                hasUa = true;

        if (!hasUa)
            Remove();
    }
}