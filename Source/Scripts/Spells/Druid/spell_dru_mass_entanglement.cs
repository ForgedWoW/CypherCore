// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(102359)]
public class SpellDruMassEntanglement : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        var targetList = new List<Unit>();
        Caster.GetAttackableUnitListInRange(targetList, 15.0f);

        if (targetList.Count != 0)
            foreach (var targets in targetList)
                Caster.AddAura(DruidSpells.MassEntanglement, targets);
    }
}