// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script] // 274738 - Ancestral Call (Mag'har Orc Racial)
internal class SpellGenAncestralCall : SpellScript, ISpellOnCast
{
    private static readonly uint[] AncestralCallBuffs =
    {
        GenericSpellIds.RICTUS_OF_THE_LAUGHING_SKULL, GenericSpellIds.ZEAL_OF_THE_BURNING_BLADE, GenericSpellIds.FEROCITY_OF_THE_FROSTWOLF, GenericSpellIds.MIGHT_OF_THE_BLACKROCK
    };


    public void OnCast()
    {
        var caster = Caster;
        var spellId = AncestralCallBuffs.SelectRandom();

        caster.SpellFactory.CastSpell(caster, spellId, true);
    }
}