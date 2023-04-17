// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(49143)]
public class SpellDkFrostStrike : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var caster = Caster;
        var target = caster.Victim;

        if (caster == null || target == null)
            return;

        if (caster.HasAura(DeathKnightSpells.ICECAP))
            if (caster.SpellHistory.HasCooldown(DeathKnightSpells.PILLAR_OF_FROST))
                caster.SpellHistory.ModifyCooldown(DeathKnightSpells.PILLAR_OF_FROST, TimeSpan.FromSeconds(-3000));

        if (caster.HasAura(DeathKnightSpells.OBLITERATION) && caster.HasAura(DeathKnightSpells.PILLAR_OF_FROST))
            caster.SpellFactory.CastSpell(null, DeathKnightSpells.KILLING_MACHINE, true);
    }
}