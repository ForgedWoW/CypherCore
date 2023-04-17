// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Paladin;

// 114852 - Holy Prism (Damage)
[SpellScript(new uint[]
{
    114852, 114871
})] // 114871 - Holy Prism (Heal)
internal class SpellPalHolyPrismSelector : SpellScript, IHasSpellEffects
{
    private List<WorldObject> _sharedTargets = new();
    private ObjectGuid _targetGUID;
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        if (ScriptSpellId == PaladinSpells.HOLY_PRISM_TARGET_ENEMY)
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaAlly));
        else if (ScriptSpellId == PaladinSpells.HOLY_PRISM_TARGET_ALLY)
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaEnemy));

        SpellEffects.Add(new ObjectAreaTargetSelectHandler(ShareTargets, 2, Targets.UnitDestAreaEntry));

        SpellEffects.Add(new EffectHandler(SaveTargetGuid, 0, SpellEffectName.Any, SpellScriptHookType.EffectHitTarget));
        SpellEffects.Add(new EffectHandler(HandleScript, 2, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void SaveTargetGuid(int effIndex)
    {
        _targetGUID = HitUnit.GUID;
    }

    private void FilterTargets(List<WorldObject> targets)
    {
        byte maxTargets = 5;

        if (targets.Count > maxTargets)
        {
            if (SpellInfo.Id == PaladinSpells.HOLY_PRISM_TARGET_ALLY)
            {
                targets.Sort(new HealthPctOrderPred());
                targets.Resize(maxTargets);
            }
            else
                targets.RandomResize(maxTargets);
        }

        _sharedTargets = targets;
    }

    private void ShareTargets(List<WorldObject> targets)
    {
        targets.Clear();
        targets.AddRange(_sharedTargets);
    }

    private void HandleScript(int effIndex)
    {
        var initialTarget = Global.ObjAccessor.GetUnit(Caster, _targetGUID);

        initialTarget?.SpellFactory.CastSpell(HitUnit, PaladinSpells.HOLY_PRISM_TARGET_BEAM_VISUAL, true);
    }
}