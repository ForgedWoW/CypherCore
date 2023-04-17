// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Evoker;

[AreaTriggerScript(EvokerAreaTriggers.BLACK_LANDSLIDE)]
public class AtEvokerLandslide : AreaTriggerScript, IAreaTriggerOverrideCreateProperties, IAreaTriggerOnInitialize, IAreaTriggerOnCreate, IAreaTriggerOnUpdate
{
    uint _castInterval;

    public AreaTriggerCreateProperties AreaTriggerCreateProperties { get; } = AreaTriggerCreateProperties.CreateDefault(EvokerAreaTriggers.RED_FIRE_STORM);

    public void OnCreate()
    {
        var caster = At.OwnerUnit;

        if (caster == null || !caster.IsPlayer)
            return;

        var pos = new WorldLocation(At.Location);
        At.SetDestination(1000, pos, caster);
    }

    public void OnInitialize()
    {
        _castInterval = 100;
        var ata = AreaTriggerCreateProperties.Template.Actions[0];
        ata.TargetType = Framework.Constants.AreaTriggerActionUserTypes.Enemy;
        AreaTriggerCreateProperties.Shape.TriggerType = Framework.Constants.AreaTriggerTypes.Sphere;
        AreaTriggerCreateProperties.Shape.SphereDatas = new AreaTriggerData.spheredatas();
        AreaTriggerCreateProperties.Shape.SphereDatas.Radius = 3;
    }

    public void OnUpdate(uint diff)
    {
        var caster = At.OwnerUnit;

        if (caster == null || !caster.IsPlayer)
            return;

        if (_castInterval <= diff)
        {
            caster.SpellFactory.CastSpell(At.Location, EvokerSpells.BLACK_LANDSLIDE_ROOT, true);
            _castInterval += 100;
        }
        else
            _castInterval -= diff;
    }
}