// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Paladin;

[SpellScript(642)] // 642 - Divine Shield
internal class SpellPalDivineShield : SpellScript, ISpellCheckCast, ISpellAfterCast
{
    public void AfterCast()
    {
        var caster = Caster;

        if (caster.HasAura(PaladinSpells.FINAL_STAND))
            caster.SpellFactory.CastSpell((Unit)null, PaladinSpells.FINAL_STAND_EFFECT, true);


        caster.SpellFactory.CastSpell(caster, PaladinSpells.FORBEARANCE, true);
        caster.SpellFactory.CastSpell(caster, PaladinSpells.IMMUNE_SHIELD_MARKER, true);
    }


    public SpellCastResult CheckCast()
    {
        if (Caster.HasAura(PaladinSpells.FORBEARANCE))
            return SpellCastResult.TargetAurastate;

        return SpellCastResult.SpellCastOk;
    }
}