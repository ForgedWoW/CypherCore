// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Evoker;

//AT ID : 23318
//Spell ID : 355913
[AreaTriggerScript(EvokerAreaTriggers.GREEN_EMERALD_BLOSSOM)]
public class AtEvokerEmeraldBlossom : AreaTriggerScript, IAreaTriggerOnRemove
{
    public void OnRemove()
    {
        var caster = At.GetCaster();

        if (caster == null)
            return;

        var args = new CastSpellExtraArgs(TriggerCastFlags.TriggeredAllowProc);
        caster.SpellFactory.CastSpell(At.Location, EvokerSpells.GREEN_EMERALD_BLOSSOM_HEAL, args);
    }
}