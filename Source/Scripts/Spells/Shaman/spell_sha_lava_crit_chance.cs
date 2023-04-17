// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// 285466 - Lava Burst Overload Damage
[SpellScript(285466)]
internal class SpellShaLavaCritChance : SpellScript, ISpellCalcCritChance
{
    public void CalcCritChance(Unit victim, ref double critChance)
    {
        var caster = Caster;

        if (caster == null ||
            victim == null)
            return;

        if (caster.HasAura(ShamanSpells.LAVA_BURST_RANK2) &&
            victim.HasAura(ShamanSpells.FlameShock, caster.GUID))
            if (victim.GetTotalAuraModifier(AuraType.ModAttackerSpellAndWeaponCritChance) > -100)
                critChance = 100.0f;
    }
}