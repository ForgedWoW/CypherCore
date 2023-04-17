// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Framework.Constants;

namespace Scripts.Spells.Monk;

[SpellScript(116095)]
public class AuraMonkDisable : AuraScript, IAuraCheckProc
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