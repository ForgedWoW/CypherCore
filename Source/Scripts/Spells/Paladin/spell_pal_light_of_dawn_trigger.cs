// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Paladin;

// 185984 - Light of Dawn aoe heal
[SpellScript(185984)]
public class SpellPalLightOfDawnTrigger : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaAlly));
        SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
    }

    private void FilterTargets(List<WorldObject> targets)
    {
        var caster = Caster;
        byte limit = 5;

        targets.RemoveIf((WorldObject target) =>
        {
            Position pos = target.Location;

            return !(caster.IsWithinDist2d(pos, 15.0f) && caster.IsInFront(target, (float)(Math.PI / 3)));
        });

        targets.RandomResize(limit);
    }

    private void HandleOnHit(int effIndex)
    {
        var dmg = HitHeal;
        dmg += Caster.UnitData.AttackPower * 1.8f;

        HitHeal = dmg;
    }
}