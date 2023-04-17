// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// 192223 - Liquid Magma Totem (erupting hit spell)
[SpellScript(192223)]
internal class SpellShaLiquidMagmaTotem : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(HandleTargetSelect, 0, Targets.UnitDestAreaEnemy));
        SpellEffects.Add(new EffectHandler(HandleEffectHitTarget, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleEffectHitTarget(int effIndex)
    {
        var hitUnit = HitUnit;

        if (hitUnit != null)
            Caster.SpellFactory.CastSpell(hitUnit, ShamanSpells.LIQUID_MAGMA_HIT, true);
    }

    private void HandleTargetSelect(List<WorldObject> targets)
    {
        // choose one random Target from targets
        if (targets.Count > 1)
        {
            var selected = targets.SelectRandom();
            targets.Clear();
            targets.Add(selected);
        }
    }
}