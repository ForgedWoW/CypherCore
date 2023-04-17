// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// 207778 - Downpour
[SpellScript(207778)]
internal class SpellShaDownpour : SpellScript, ISpellAfterCast, ISpellAfterHit, IHasSpellEffects
{
    private int _healedTargets = 0;

    public List<ISpellEffect> SpellEffects { get; } = new();


    public void AfterCast()
    {
        var cooldown = TimeSpan.FromMilliseconds(SpellInfo.RecoveryTime) + TimeSpan.FromSeconds(GetEffectInfo(1).CalcValue() * _healedTargets);
        Caster.SpellHistory.StartCooldown(SpellInfo, 0, Spell, false, cooldown);
    }

    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaAlly));
    }

    public void AfterHit()
    {
        // Cooldown increased for each Target effectively healed
        if (HitHeal != 0)
            ++_healedTargets;
    }

    private void FilterTargets(List<WorldObject> targets)
    {
        SelectRandomInjuredTargets(targets, 6, true);
    }
}