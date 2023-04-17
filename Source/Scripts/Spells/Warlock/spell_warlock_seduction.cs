﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// 6358 - Seduction, 115268 - Mesmerize
[SpellScript(new uint[]
{
    6358, 115268
})]
public class SpellWarlockSeduction : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.ModStun, AuraEffectHandleModes.Real));
    }

    private void OnApply(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        // Glyph of Demon Training
        var target = Target;
        var caster = Caster;

        if (caster == null)
            return;

        var owner = caster.OwnerUnit;

        if (owner != null)
            if (owner.HasAura(WarlockSpells.GLYPH_OF_DEMON_TRAINING))
            {
                target.RemoveAurasByType(AuraType.PeriodicDamage);
                target.RemoveAurasByType(AuraType.PeriodicDamagePercent);
            }

        // remove invisibility from Succubus on successful cast
        caster.RemoveAura(WarlockSpells.PET_LESSER_INVISIBILITY);
    }
}