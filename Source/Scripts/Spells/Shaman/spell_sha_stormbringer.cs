// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// Stormbringer - 201845
[SpellScript(201845)]
public class SpellShaStormbringer : AuraScript, IAuraCheckProc, IAuraOnProc
{
    public bool CheckProc(ProcEventInfo eventInfo)
    {
        return eventInfo.DamageInfo.AttackType == WeaponAttackType.BaseAttack;
    }

    public void OnProc(ProcEventInfo info)
    {
        var caster = Caster;

        if (caster != null)
        {
            caster.SpellFactory.CastSpell(caster, ShamanSpells.STORMBRINGER_PROC, true);
            caster.SpellHistory.ResetCooldown(ShamanSpells.STORMSTRIKE, true);
        }
    }
}