﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin;

// 1022 - Blessing of Protection
// 204018 - Blessing of Spellwarding
[SpellScript(new uint[]
{
    1022, 204018
})]
internal class spell_pal_blessing_of_protection : SpellScript, ISpellCheckCast, ISpellAfterHit
{
    public void AfterHit()
    {
        var target = HitUnit;

        if (target)
        {
            Caster.CastSpell(target, PaladinSpells.Forbearance, true);
            Caster.CastSpell(target, PaladinSpells.ImmuneShieldMarker, true);
        }
    }

    public SpellCastResult CheckCast()
    {
        var target = ExplTargetUnit;

        if (!target ||
            target.HasAura(PaladinSpells.Forbearance))
            return SpellCastResult.TargetAurastate;

        return SpellCastResult.SpellCastOk;
    }
}