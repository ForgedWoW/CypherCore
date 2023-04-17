// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

// Defensive Stance - 71
[SpellScript(71)]
public class SpellWarrDefensiveStance : AuraScript, IAuraOnProc
{
    private double _damageTaken = 0;

    public void OnProc(ProcEventInfo eventInfo)
    {
        var caster = Caster;

        if (caster == null)
            return;

        _damageTaken = eventInfo.DamageInfo != null ? eventInfo.DamageInfo.Damage : 0;

        if (_damageTaken <= 0)
            return;

        var rageAmount = (int)((50.0f * _damageTaken) / caster.MaxHealth);
        caster.ModifyPower(PowerType.Rage, 10 * rageAmount);
    }
}