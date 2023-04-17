// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(115313)]
public class SpellMonkJadeSerpentStatue : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        var caster = Caster;

        if (caster == null)
            return;

        var player = caster.AsPlayer;

        if (player == null)
            return;

        var serpentStatueList = player.GetCreatureListWithEntryInGrid(MonkSpells.MONK_NPC_JADE_SERPENT_STATUE, 500.0f);

        serpentStatueList.RemoveIf(c => c.OwnerUnit == null || c.OwnerUnit != player || !c.IsSummon);

        if (serpentStatueList.Count >= 1)
            serpentStatueList.Last().ToTempSummon().UnSummon();
    }
}