// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.DemonHunter;

/// Honor Talents

// Solitude - 211509
[SpellScript(211509)]
public class SpellDhSolitude : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDummy));
    }

    private void HandlePeriodic(AuraEffect unnamedParameter)
    {
        PreventDefaultAction();

        var caster = Caster;

        if (caster == null || !SpellInfo.GetEffect(1).IsEffect)
            return;

        var range = (float)SpellInfo.GetEffect(1).BasePoints;
        var allies = new List<Unit>();
        var check = new AnyFriendlyUnitInObjectRangeCheck(caster, caster, range, true);
        var searcher = new UnitListSearcher(caster, allies, check, GridType.All);
        Cell.VisitGrid(caster, searcher, range);
        allies.Remove(caster);

        if (allies.Count == 0 && !caster.HasAura(DemonHunterSpells.SOLITUDE_BUFF))
            caster.SpellFactory.CastSpell(caster, DemonHunterSpells.SOLITUDE_BUFF, true);
        else if (allies.Count > 0)
            caster.RemoveAura(DemonHunterSpells.SOLITUDE_BUFF);
    }
}