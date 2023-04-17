// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Evoker;

[AreaTriggerScript(EvokerAreaTriggers.GREEN_EMERALD_BLOSSOM)]
public class AtEvokerFlutteringSeedling : AreaTriggerScript, IAreaTriggerOnRemove
{
    public void OnRemove()
    {
        var caster = At.GetCaster();

        if (caster == null)
            return;

        if (!caster.TryGetAura(EvokerSpells.FLUTTERING_SEEDLINGS, out var fsAura))
            return;

        // get targets
        var targetList = new List<Unit>();
        caster.GetAlliesWithinRange(targetList, (float)fsAura.SpellInfo.GetEffect(1).BasePoints);

        // reduce targetList to the number allowed
        targetList.RandomResize(fsAura.GetEffect(0).Amount);

        // cast on targets
        foreach (var target in targetList)
            caster.SpellFactory.CastSpell(target, EvokerSpells.FLUTTERING_SEEDLINGS_HEAL, true);
    }
}