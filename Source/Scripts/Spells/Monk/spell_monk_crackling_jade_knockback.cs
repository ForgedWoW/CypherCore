// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(117962)]
public class SpellMonkCracklingJadeKnockback : SpellScript, ISpellAfterHit
{
    public void AfterHit()
    {
        var target = HitUnit;
        var caster = Caster;

        if (caster != null && target != null && caster.HasAura(CracklingJade.CracklingJadeLightningTalent))
            caster.SpellFactory.CastSpell(target, CracklingJade.CracklingJadLightningTalentSpeed, true);
    }
}