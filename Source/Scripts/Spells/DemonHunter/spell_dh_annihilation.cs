// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.DemonHunter;

[SpellScript(201427)]
public class SpellDhAnnihilation : SpellScript, ISpellBeforeHit
{
    public void BeforeHit(SpellMissInfo missInfo)
    {
        var caster = Caster;

        if (caster != null)
        {
            var target = caster.Victim;

            if (target == null)
                return;

            var attackPower = caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) + 28.7f;
            var damage = HitDamage;

            HitDamage = damage + attackPower;

            if (RandomHelper.randChance(20))
                caster.ModifyPower(PowerType.Fury, +20);
        }
    }
}