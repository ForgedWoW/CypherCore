// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[Script] // 48438 - Wild Growth
internal class SpellDruWildGrowth : SpellScript, IHasSpellEffects
{
    private List<WorldObject> _targets;
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaAlly));
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(SetTargets, 1, Targets.UnitDestAreaAlly));
    }

    private void FilterTargets(List<WorldObject> targets)
    {
        targets.RemoveAll(obj =>
        {
            var target = obj.AsUnit;

            if (target)
                return !Caster.IsInRaidWith(target);

            return true;
        });

        var maxTargets = (int)GetEffectInfo(1).CalcValue(Caster);

        if (targets.Count > maxTargets)
        {
            targets.Sort(new HealthPctOrderPred());
            targets.RemoveRange(maxTargets, targets.Count - maxTargets);
        }

        _targets = targets;
    }

    private void SetTargets(List<WorldObject> targets)
    {
        targets.Clear();
        targets.AddRange(_targets);
    }
}