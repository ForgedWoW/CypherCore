// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenCannibalize : SpellScript, ISpellCheckCast, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public SpellCastResult CheckCast()
    {
        var caster = Caster;
        var maxRange = SpellInfo.GetMaxRange(false);
        // search for nearby enemy corpse in range
        var check = new AnyDeadUnitSpellTargetInRangeCheck<Unit>(caster, maxRange, SpellInfo, SpellTargetCheckTypes.Enemy, SpellTargetObjectTypes.CorpseEnemy);
        var searcher = new UnitSearcher(caster, check, GridType.Grid);
        Cell.VisitGrid(caster, searcher, maxRange);

        if (!searcher.GetTarget())
        {
            searcher.GridType = GridType.World;
            Cell.VisitGrid(caster, searcher, maxRange);
        }

        if (!searcher.GetTarget())
            return SpellCastResult.NoEdibleCorpses;

        return SpellCastResult.SpellCastOk;
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
    }

    private void HandleDummy(int effIndex)
    {
        Caster.SpellFactory.CastSpell(Caster, GenericSpellIds.CANNIBALIZE_TRIGGERED, false);
    }
}