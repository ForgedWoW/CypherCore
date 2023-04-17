// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// Flametongue Attack - 10444
[SpellScript(10444)]
public class BfaSpellFlametongueAttackDamage : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var caster = Caster;

        if (caster == null)
            return;

        var flamet = caster.GetAura(ShamanSpells.FLAMETONGUE_AURA);

        if (flamet != null)
            HitDamage = (int)(caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 0.2f);
    }
}