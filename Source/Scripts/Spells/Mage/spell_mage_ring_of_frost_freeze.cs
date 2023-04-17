// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Mage;

[Script] // 82691 - Ring of Frost (freeze efect)
internal class SpellMageRingOfFrostFreeze : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaEnemy));
    }

    private void FilterTargets(List<WorldObject> targets)
    {
        var dest = ExplTargetDest;
        var outRadius = Global.SpellMgr.GetSpellInfo(MageSpells.RING_OF_FROST_SUMMON, CastDifficulty).GetEffect(0).CalcRadius();
        var inRadius = 6.5f;

        targets.RemoveAll(target =>
        {
            var unit = target.AsUnit;

            if (!unit)
                return true;

            return unit.HasAura(MageSpells.RING_OF_FROST_DUMMY) || unit.HasAura(MageSpells.RingOfFrostFreeze) || unit.Location.GetExactDist(dest) > outRadius || unit.Location.GetExactDist(dest) < inRadius;
        });
    }
}