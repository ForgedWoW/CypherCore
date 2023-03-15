// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Evoker;

[AreaTriggerScript(EvokerAreaTriggers.BLACK_LANDSLIDE)]
public class at_evoker_landslide : AreaTriggerScript, IAreaTriggerOverrideCreateProperties, IAreaTriggerOnInitialize, IAreaTriggerOnUnitEnter
{
    public AreaTriggerCreateProperties AreaTriggerCreateProperties { get; } = AreaTriggerCreateProperties.CreateDefault(EvokerAreaTriggers.RED_FIRE_STORM);

    public void OnInitialize()
    {
        var ata = AreaTriggerCreateProperties.Template.Actions[0];
        ata.TargetType = Framework.Constants.AreaTriggerActionUserTypes.Enemy;
    }

    public void OnUnitEnter(Unit unit)
    {
        At.OwnerUnit.CastSpell(unit, EvokerSpells.BLACK_LANDSLIDE_ROOT);
    }
}