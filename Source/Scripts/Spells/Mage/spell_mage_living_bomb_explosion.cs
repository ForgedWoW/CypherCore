// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 44461 - Living Bomb
internal class spell_mage_living_bomb_explosion : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaEnemy));
        SpellEffects.Add(new EffectHandler(HandleSpread, 1, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void FilterTargets(List<WorldObject> targets)
    {
        targets.Remove(ExplTargetWorldObject);
    }

    private void HandleSpread(int effIndex)
    {
        if (SpellValue.EffectBasePoints[0] > 0)
            Caster.CastSpell(HitUnit, MageSpells.LivingBombPeriodic, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint2, 0));
    }
}