﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin;

[SpellScript(642)] // 642 - Divine Shield
internal class spell_pal_divine_shield : SpellScript, ISpellCheckCast, ISpellAfterCast
{
    public void AfterCast()
    {
        var caster = Caster;

        if (caster.HasAura(PaladinSpells.FinalStand))
            caster.CastSpell((Unit)null, PaladinSpells.FinalStandEffect, true);


        caster.CastSpell(caster, PaladinSpells.Forbearance, true);
        caster.CastSpell(caster, PaladinSpells.ImmuneShieldMarker, true);
    }


    public SpellCastResult CheckCast()
    {
        if (Caster.HasAura(PaladinSpells.Forbearance))
            return SpellCastResult.TargetAurastate;

        return SpellCastResult.SpellCastOk;
    }
}