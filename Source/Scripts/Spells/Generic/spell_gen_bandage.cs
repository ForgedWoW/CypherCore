// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenBandage : SpellScript, ISpellCheckCast, ISpellAfterHit
{
    public void AfterHit()
    {
        var target = HitUnit;

        if (target)
            Caster.SpellFactory.CastSpell(target, GenericSpellIds.RECENTLY_BANDAGED, true);
    }


    public SpellCastResult CheckCast()
    {
        var target = ExplTargetUnit;

        if (target)
            if (target.HasAura(GenericSpellIds.RECENTLY_BANDAGED))
                return SpellCastResult.TargetAurastate;

        return SpellCastResult.SpellCastOk;
    }
}