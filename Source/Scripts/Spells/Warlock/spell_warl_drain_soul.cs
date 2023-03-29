// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

[SpellScript(198590)] // 198590 - Drain Soul
internal class spell_warl_drain_soul : SpellScript, ISpellCalculateMultiplier
{
    public double CalcMultiplier(double multiplier)
    {
        if (Caster.HasAuraState(AuraStateType.Wounded20Percent))
            multiplier *= 2;

        return multiplier;
    }
}