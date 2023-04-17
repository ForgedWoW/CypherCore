// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Scripts.Spells.DeathKnight;

[SpellScript(77606)]
public class DarkSimulacrum : AuraScript, IAuraOnProc
{
    public void OnProc(ProcEventInfo info)
    {
        var spellInfo = info.SpellInfo;
        var player = Caster.AsPlayer;
        var target = Target;

        if (spellInfo != null && player != null && target != null && target.IsValidAttackTarget(player, spellInfo))
            player.SpellFactory.CastSpell(target, spellInfo.Id, true);
    }
}