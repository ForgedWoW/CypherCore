// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Paladin;

// 1022 - Blessing of Protection
// 204018 - Blessing of Spellwarding
[SpellScript(new uint[]
{
    1022, 204018
})]
internal class SpellPalBlessingOfProtection : SpellScript, ISpellCheckCast, ISpellAfterHit
{
    public void AfterHit()
    {
        var target = HitUnit;

        if (target)
        {
            Caster.SpellFactory.CastSpell(target, PaladinSpells.FORBEARANCE, true);
            Caster.SpellFactory.CastSpell(target, PaladinSpells.IMMUNE_SHIELD_MARKER, true);
        }
    }

    public SpellCastResult CheckCast()
    {
        var target = ExplTargetUnit;

        if (!target ||
            target.HasAura(PaladinSpells.FORBEARANCE))
            return SpellCastResult.TargetAurastate;

        return SpellCastResult.SpellCastOk;
    }
}