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

namespace Scripts.Spells.Warlock;

// Channel Demonfire - 196447
[SpellScript(196447)]
public class SpellWarlChannelDemonfire : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDummy));
    }

    private void HandlePeriodic(AuraEffect unnamedParameter)
    {
        var caster = Caster;
        var rangeInfoSpell = Global.SpellMgr.GetSpellInfo(WarlockSpells.CHANNEL_DEMONFIRE_RANGE);

        if (caster == null)
            return;

        var enemies = new List<Unit>();
        var check = new AnyUnfriendlyUnitInObjectRangeCheck(caster, caster, rangeInfoSpell.GetMaxRange(), new UnitAuraCheck<Unit>(true, WarlockSpells.IMMOLATE_DOT, caster.GUID).Invoke);
        var searcher = new UnitListSearcher(caster, enemies, check, GridType.All);
        Cell.VisitGrid(caster, searcher, rangeInfoSpell.GetMaxRange());

        if (enemies.Count == 0)
            return;

        var target = enemies.SelectRandom();
        caster.SpellFactory.CastSpell(target, WarlockSpells.CHANNEL_DEMONFIRE_DAMAGE, true);
    }
}