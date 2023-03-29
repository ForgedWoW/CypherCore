// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Monk;

[Script]
public class at_monk_song_of_chiji : AreaTriggerScript, IAreaTriggerOnUnitEnter
{
    public void OnUnitEnter(Unit unit)
    {
        var caster = At.GetCaster();

        if (caster == null || unit == null)
            return;

        if (!caster.AsPlayer)
            return;

        if (unit != caster && caster.IsValidAttackTarget(unit))
            caster.CastSpell(unit, MonkSpells.SONG_OF_CHIJI, true);
    }
}