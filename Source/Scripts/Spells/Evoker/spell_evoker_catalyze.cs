﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.BLUE_DISINTEGRATE, EvokerSpells.BLUE_DISINTEGRATE_2)]
public class SpellEvokerCatalyze : SpellScript, ISpellOnHit, ISpellAfterCast
{
    int _period = 0;

    public void AfterCast()
    {
        if (Caster.TryGetAsPlayer(out var player) && player.HasSpell(EvokerSpells.CATALYZE))
            if (HitUnit.TryGetAura(EvokerSpells.RED_FIRE_BREATH_CHARGED, out var aura))
                aura.GetEffect(1).Period = _period;
    }

    public void OnHit()
    {
        if (Caster.TryGetAsPlayer(out var player) && player.HasSpell(EvokerSpells.CATALYZE))
            if (HitUnit.TryGetAura(EvokerSpells.RED_FIRE_BREATH_CHARGED, out var aura))
            {
                var eff = aura.GetEffect(1);
                _period = eff.Period;
                eff.Period = _period / 2;
            }
    }
}