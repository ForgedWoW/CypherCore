﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Monk;

[SpellScript(116095)]
public class aura_monk_disable : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo eventInfo)
    {
        var damageInfo = eventInfo.DamageInfo;

        if (damageInfo != null)
            if ((damageInfo.AttackType == WeaponAttackType.BaseAttack || damageInfo.AttackType == WeaponAttackType.OffAttack) && damageInfo.Attacker == Caster)
            {
                Aura.RefreshDuration();

                return true;
            }

        return false;
    }
}