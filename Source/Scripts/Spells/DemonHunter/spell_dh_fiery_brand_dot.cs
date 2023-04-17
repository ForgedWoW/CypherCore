// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.DemonHunter;

[SpellScript(207771)]
public class SpellDhFieryBrandDot : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 2, AuraType.PeriodicDamage));
    }

    private void PeriodicTick(AuraEffect aurEff)
    {
        var caster = Caster;

        if (caster == null || !caster.HasAura(DemonHunterSpells.BURNING_ALIVE))
            return;

        var unitList = new List<Unit>();
        Target.GetAnyUnitListInRange(unitList, 8.0f);

        foreach (var target in unitList)
            if (!target.HasAura(DemonHunterSpells.FIERY_BRAND_DOT) && !target.HasAura(DemonHunterSpells.FIERY_BRAND_MARKER) && !caster.IsFriendlyTo(target))
            {
                caster.SpellFactory.CastSpell(target, DemonHunterSpells.FIERY_BRAND_MARKER, true);

                break;
            }
    }
}