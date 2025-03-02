﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.ECHO)]
public class spell_evoker_snapfire : SpellScript, ISpellCalculateMultiplier
{
    public double CalcMultiplier(double multiplier)
    {
        if (Caster.TryGetAsPlayer(out var player) && player.TryGetAura(EvokerSpells.SNAPFIRE_AURA, out var aura))
            multiplier *= 1 + (aura.GetEffect(1).Amount * 0.01);

        return multiplier;
    }
}