// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 71610, 71641 - Echoes of Light (Althor's Abacus)
internal class SpellItemEchoesOfLight : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaAlly));
    }

    private void FilterTargets(List<WorldObject> targets)
    {
        if (targets.Count < 2)
            return;

        targets.Sort(new HealthPctOrderPred());

        var target = targets.FirstOrDefault();
        targets.Clear();
        targets.Add(target);
    }
}