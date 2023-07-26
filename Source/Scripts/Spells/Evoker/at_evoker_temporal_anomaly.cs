// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;
using Forged.MapServer.Spells;

namespace Scripts.Spells.Evoker;

[AreaTriggerScript(EvokerAreaTriggers.BRONZE_TEMPORAL_ANOMALY)]
public class AtEvokerTemporalAnomaly : AreaTriggerScript, IAreaTriggerOverrideCreateProperties, IAreaTriggerOnInitialize, IAreaTriggerOnCreate, IAreaTriggerOnUnitEnter
{
    readonly List<Unit> _hit = new();
    double _amount = 0;
    int _targets = 0;

    public AreaTriggerCreateProperties AreaTriggerCreateProperties { get; }

    public AtEvokerTemporalAnomaly(ScriptManager scriptManager)
    {
        AreaTriggerCreateProperties = AreaTriggerCreateProperties.CreateDefault(EvokerAreaTriggers.BRONZE_TEMPORAL_ANOMALY, scriptManager);
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

    public void OnInitialize()
    {
        AreaTriggerCreateProperties.Shape.TriggerType = Framework.Constants.AreaTriggerTypes.Sphere;
        AreaTriggerCreateProperties.Shape.SphereDatas = new AreaTriggerData.spheredatas();
        AreaTriggerCreateProperties.Shape.SphereDatas.Radius = 3;
    }

    public void OnUnitEnter(Unit unit)
    {
        var caster = At.GetCaster().AsPlayer;

        if (caster.IsFriendlyTo(unit) && !_hit.Contains(unit))
        {
            _hit.Add(unit);
            caster.SpellFactory.CastSpell(unit, EvokerSpells.BRONZE_TEMPORAL_ANOMALY_AURA, _amount / (_targets > 0 ? 1 : 2), true);
            _targets--;
        }
    }
}