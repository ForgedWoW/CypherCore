// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;
using Scripts.Spells.Mage;
using static Game.AI.SmartEvent;

namespace Scripts.Spells.Evoker;

[AreaTriggerScript(EvokerAreaTriggers.BLACK_LANDSLIDE)]
public class at_evoker_landslide : AreaTriggerScript, IAreaTriggerOverrideCreateProperties, IAreaTriggerOnInitialize, IAreaTriggerOnCreate, IAreaTriggerOnUpdate
{
    uint _castInterval;

    public AreaTriggerCreateProperties AreaTriggerCreateProperties { get; } = AreaTriggerCreateProperties.CreateDefault(EvokerAreaTriggers.RED_FIRE_STORM);

    public void OnInitialize()
    {
        _castInterval = 100;
        var ata = AreaTriggerCreateProperties.Template.Actions[0];
        ata.TargetType = Framework.Constants.AreaTriggerActionUserTypes.Enemy;
        AreaTriggerCreateProperties.Shape.TriggerType = Framework.Constants.AreaTriggerTypes.Sphere;
        AreaTriggerCreateProperties.Shape.SphereDatas = new AreaTriggerData.spheredatas();
        AreaTriggerCreateProperties.Shape.SphereDatas.Radius = 3;
    }

    public void OnCreate()
    {
        var caster = At.OwnerUnit;

        if (caster == null || !caster.IsPlayer)
            return;
        
        var pos = new WorldLocation(At.Location);
        At.SetDestination(1000, pos, caster);
    }

    public void OnUpdate(uint diff)
    {
        var caster = At.OwnerUnit;

        if (caster == null || !caster.IsPlayer)
            return;

        if (_castInterval <= diff)
        {
            caster.CastSpell(At.Location, EvokerSpells.BLACK_LANDSLIDE_ROOT, true);
            _castInterval += 100;
        }
        else
            _castInterval -= diff;
    }
}