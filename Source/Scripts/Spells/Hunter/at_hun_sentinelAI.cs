// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;
using Framework.Constants;

namespace Scripts.Spells.Hunter;

[Script]
public class AtHunSentinelAI : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnUpdate, IAreaTriggerOnRemove
{
    public int TimeInterval;

    public void OnCreate()
    {
        TimeInterval = 6000;
    }

    public void OnRemove()
    {
        var caster = At.GetCaster();

        if (caster != null)
        {
            var targetList = new List<Unit>();
            var radius = Global.SpellMgr.GetSpellInfo(HunterSpells.SENTINEL, Difficulty.None).GetEffect(0).CalcRadius(caster);

            var lCheck = new AnyUnitInObjectRangeCheck(At, radius);
            var lSearcher = new UnitListSearcher(At, targetList, lCheck, GridType.All);
            Cell.VisitGrid(At, lSearcher, radius);

            foreach (var lUnit in targetList)
                if (lUnit != caster && caster.IsValidAttackTarget(lUnit))
                {
                    caster.SpellFactory.CastSpell(lUnit, HunterSpells.HUNTERS_MARK_AURA, true);
                    caster.SpellFactory.CastSpell(caster, HunterSpells.HUNTERS_MARK_AURA_2, true);

                    TimeInterval -= 6000;
                }
        }
    }

    public void OnUpdate(uint diff)
    {
        TimeInterval += (int)diff;

        if (TimeInterval < 6000)
            return;

        var caster = At.GetCaster();

        if (caster != null)
        {
            var targetList = new List<Unit>();
            var radius = Global.SpellMgr.GetSpellInfo(HunterSpells.SENTINEL, Difficulty.None).GetEffect(0).CalcRadius(caster);

            var lCheck = new AnyUnitInObjectRangeCheck(At, radius);
            var lSearcher = new UnitListSearcher(At, targetList, lCheck, GridType.All);
            Cell.VisitGrid(At, lSearcher, radius);

            foreach (var lUnit in targetList)

            {
                caster.SpellFactory.CastSpell(lUnit, HunterSpells.HUNTERS_MARK_AURA, true);
                caster.SpellFactory.CastSpell(caster, HunterSpells.HUNTERS_MARK_AURA_2, true);

                TimeInterval -= 6000;
            }
        }
    }
}