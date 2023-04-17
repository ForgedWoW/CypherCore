// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Monk;

[SpellScript(194011)]
public class SpellMonkZenPilgrimage : SpellScript, ISpellOnCast, ISpellCheckCast
{
    public SpellCastResult CheckCast()
    {
        if (SpellInfo.Id == 194011)
            return SpellCastResult.SpellCastOk;

        var caster = Caster;

        if (caster != null)
        {
            var player = caster.AsPlayer;

            if (player != null)
                if (player.IsQuestRewarded(40236)) // Check quest for port to oplot
                {
                    caster.SpellFactory.CastSpell(caster, 194011, false);

                    return SpellCastResult.DontReport;
                }
        }

        return SpellCastResult.SpellCastOk;
    }

    public void OnCast()
    {
        var caster = Caster;

        if (caster != null)
        {
            var player = caster.AsPlayer;

            if (player != null)
            {
                player.SaveRecallPosition();
                player.SpellFactory.CastSpell(player, 126896, true);
            }
        }
    }
}