// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin;

// Activate Forbearance
// Called by Blessing of Protection - 1022, Lay on Hands - 633, Blessing of Spellwarding - 204018
[SpellScript(new uint[]
{
    1022, 633, 204018
})]
public class spell_pal_activate_forbearance : SpellScript, ISpellOnHit, ISpellCheckCast
{
    public SpellCastResult CheckCast()
    {
        var target = ExplTargetUnit;

        if (target != null)
            if (target.HasAura(PaladinSpells.Forbearance))
                return SpellCastResult.TargetAurastate;

        return SpellCastResult.SpellCastOk;
    }


    public void OnHit()
    {
        var player = Caster.AsPlayer;

        if (player != null)
        {
            var target = HitUnit;

            if (target != null)
                player.CastSpell(target, PaladinSpells.Forbearance, true);
        }
    }
}