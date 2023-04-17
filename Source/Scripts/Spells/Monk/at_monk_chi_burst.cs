// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Monk;

[Script]
public class AtMonkChiBurst : AreaTriggerScript, IAreaTriggerOnUnitEnter
{
    public void OnUnitEnter(Unit target)
    {
        if (!At.GetCaster())
            return;

        if (At.GetCaster().IsValidAssistTarget(target))
            At.GetCaster().SpellFactory.CastSpell(target, MonkSpells.CHI_BURST_HEAL, true);

        if (At.GetCaster().IsValidAttackTarget(target))
            At.GetCaster().SpellFactory.CastSpell(target, MonkSpells.CHI_BURST_DAMAGE, true);
    }
}