// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Evoker;

/// <summary>
/// spell has 7 ticks in total. 1 on create. 5 within the 12s duration. 1 on remove. added _hits in cast all 6 manage to happen in OnUpdates
/// </summary>
[AreaTriggerScript(EvokerAreaTriggers.RED_FIRE_STORM)]
public class at_evoker_firestorm : AreaTriggerScript, IAreaTriggerOverrideCreateProperties, 
    IAreaTriggerOnUpdate, IAreaTriggerOnInitialize, IAreaTriggerOnCreate, IAreaTriggerOnRemove
{
    public AreaTriggerCreateProperties AreaTriggerCreateProperties { get; } = AreaTriggerCreateProperties.CreateDefault(EvokerAreaTriggers.RED_FIRE_STORM);

    uint _timer = 0;
    int _hits = 7;

    public void OnInitialize()
    {
        var ata = AreaTriggerCreateProperties.Template.Actions[0];
        ata.TargetType = Framework.Constants.AreaTriggerActionUserTypes.Enemy;
    }

    public void OnCreate()
    {
        At.GetCaster().CastSpell(At.Location, EvokerSpells.RED_FIRE_STORM_DAMAGE, true);
        _hits--;
    }

    public void OnUpdate(uint diff)
    {
        if (_hits <= 0)
            return;

        _timer += diff;


        if (_timer >= 2000) // tick every 2 seconds
        {
            At.GetCaster().CastSpell(At.Location, EvokerSpells.RED_FIRE_STORM_DAMAGE, true);
            _hits--;
            _timer -= 2000; // only subtract 2000 to carry over any extra time
        }
    }

    public void OnRemove()
    {
        if (_hits > 0)
            At.GetCaster().CastSpell(At.Location, EvokerSpells.RED_FIRE_STORM_DAMAGE, true);
    }
}