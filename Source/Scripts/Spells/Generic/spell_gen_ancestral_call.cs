﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script] // 274738 - Ancestral Call (Mag'har Orc Racial)
internal class spell_gen_ancestral_call : SpellScript, ISpellOnCast
{
    private static readonly uint[] AncestralCallBuffs =
    {
        GenericSpellIds.RictusOfTheLaughingSkull, GenericSpellIds.ZealOfTheBurningBlade, GenericSpellIds.FerocityOfTheFrostwolf, GenericSpellIds.MightOfTheBlackrock
    };


    public void OnCast()
    {
        var caster = Caster;
        var spellId = AncestralCallBuffs.SelectRandom();

        caster.CastSpell(caster, spellId, true);
    }
}