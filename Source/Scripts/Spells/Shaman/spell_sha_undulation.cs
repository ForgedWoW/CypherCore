// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman;

// Undulation
// 8004 Healing Surge
// 77472 Healing Wave
[SpellScript(new uint[]
{
    8004, 77472
})]
public class SpellShaUndulation : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var variableStore = Caster.VariableStorage;
        var count = variableStore.GetValue("spell_sha_undulation", 0);

        if (count >= 3)
        {
            variableStore.Remove("spell_sha_undulation");
            Caster.SpellFactory.CastSpell(ShamanSpells.UNDULATION_PROC, true);
        }
        else
        {
            variableStore.Set("spell_sha_undulation", count + 1);
            Caster.RemoveAura(ShamanSpells.UNDULATION_PROC);
        }
    }
}