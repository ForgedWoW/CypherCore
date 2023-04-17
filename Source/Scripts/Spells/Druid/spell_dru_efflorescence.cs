// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(145205)]
public class SpellDruEfflorescence : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        var caster = Caster;

        if (caster != null)
        {
            var efflorescence = caster.GetSummonedCreatureByEntry(ECreature.NPCEfflorescence);

            if (efflorescence != null)
                efflorescence.DespawnOrUnsummon();
        }
    }

    private struct ECreature
    {
        public static readonly uint NPCEfflorescence = 47649;
    }
}