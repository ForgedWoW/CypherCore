// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;
using System.Collections.Generic;

namespace Scripts.Spells.Evoker;

[AreaTriggerScript(EvokerAreaTriggers.BRONZE_TEMPORAL_ANOMALY)]
public class at_evoker_temporal_anomaly : AreaTriggerScript, IAreaTriggerOverrideCreateProperties, IAreaTriggerOnInitialize, IAreaTriggerOnCreate, IAreaTriggerOnUnitEnter
{
    double _amount = 0;
    int _targets = 0;
    List<Unit> _hit = new();
    
    public AreaTriggerCreateProperties AreaTriggerCreateProperties { get; } = AreaTriggerCreateProperties.CreateDefault(EvokerAreaTriggers.BRONZE_TEMPORAL_ANOMALY);

    public void OnInitialize()
    {
        AreaTriggerCreateProperties.Shape.TriggerType = Framework.Constants.AreaTriggerTypes.Sphere;
        AreaTriggerCreateProperties.Shape.SphereDatas = new AreaTriggerData.spheredatas();
        AreaTriggerCreateProperties.Shape.SphereDatas.Radius = 3;
    }

    public void OnCreate()
    {
        var caster = At.GetCaster().AsPlayer;

        if (caster == null)
            return;

        var pos = new WorldLocation(caster.Location);
        At.MovePositionToFirstCollision(pos, 40.0f, 0.0f);
        At.SetDestination((uint)At.Duration, pos);

        _amount = caster.GetTotalSpellPowerValue(Framework.Constants.SpellSchoolMask.Arcane, true) * 1.75 * (1 + caster.ActivePlayerData.VersatilityBonus.Value);
        _targets = (int)SpellManager.Instance.GetSpellInfo(EvokerSpells.BRONZE_TEMPORAL_ANOMALY).GetEffect(1).BasePoints;
    }

    public void OnUnitEnter(Unit unit)
    {
        var caster = At.GetCaster().AsPlayer;

        if (caster.IsFriendlyTo(unit) && !_hit.Contains(unit))
        {
            _hit.Add(unit);
            caster.CastSpell(unit, EvokerSpells.BRONZE_TEMPORAL_ANOMALY_AURA, _amount / (_targets > 0 ? 1 : 2));
            _targets--;
        }
    }
}