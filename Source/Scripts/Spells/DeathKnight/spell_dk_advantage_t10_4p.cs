// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[Script] // 70656 - Advantage (T10 4P Melee Bonus)
internal class SpellDkAdvantageT104P : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo eventInfo)
    {
        var caster = eventInfo.Actor;

        if (caster)
        {
            var player = caster.AsPlayer;

            if (!player ||
                caster.Class != PlayerClass.Deathknight)
                return false;

            for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
                if (player.GetRuneCooldown(i) == 0)
                    return false;

            return true;
        }

        return false;
    }
}