// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// 603 - Doom
public class SpellWarlockDoom : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicDamage));
    }

    private void PeriodicTick(AuraEffect aurEff)
    {
        var caster = Caster;

        if (caster == null)
            return;

        caster.SpellFactory.CastSpell(caster, WarlockSpells.DOOM_ENERGIZE, true);

        if (caster.HasAura(WarlockSpells.IMPENDING_DOOM))
            caster.SpellFactory.CastSpell(Target, WarlockSpells.WILD_IMP_SUMMON, true);

        if (caster.HasAura(WarlockSpells.DOOM_DOUBLED) && RandomHelper.randChance(25))
            GetEffect(0).SetAmount(aurEff.Amount * 2);
    }
}