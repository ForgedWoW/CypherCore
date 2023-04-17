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

[SpellScript(54968)] // 54968 - Glyph of Holy Light
internal class SpellPalGlyphOfHolyLight : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaAlly));
    }

    private void FilterTargets(List<WorldObject> targets)
    {
        var maxTargets = SpellInfo.MaxAffectedTargets;

        if (targets.Count > maxTargets)
        {
            targets.Sort(new HealthPctOrderPred());
            targets.Resize(maxTargets);
        }
    }
}