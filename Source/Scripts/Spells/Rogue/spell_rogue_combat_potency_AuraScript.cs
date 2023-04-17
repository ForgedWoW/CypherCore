// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Framework.Constants;

namespace Scripts.Spells.Rogue;

[SpellScript(35551)]
public class SpellRogueCombatPotencyAuraScript : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo eventInfo)
    {
        var offHand = (eventInfo.DamageInfo.AttackType == WeaponAttackType.OffAttack && RandomHelper.randChance(20));
        var mainRollChance = 20.0f * Caster.GetAttackTimer(WeaponAttackType.BaseAttack) / 1.4f / 600.0f;
        var mainHand = (eventInfo.DamageInfo.AttackType == WeaponAttackType.BaseAttack && RandomHelper.randChance(mainRollChance));

        return offHand || mainHand;
    }
}