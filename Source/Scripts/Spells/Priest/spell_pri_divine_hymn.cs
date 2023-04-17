// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Priest;

[Script] // 64844 - Divine Hymn
internal class SpellPriDivineHymn : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, SpellConst.EffectAll, Targets.UnitSrcAreaAlly));
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

        uint maxTargets = 3;

        if (targets.Count > maxTargets)
        {
            targets.Sort(new HealthPctOrderPred());
            targets.Resize(maxTargets);
        }
    }
}