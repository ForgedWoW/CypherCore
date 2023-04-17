// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Monk;

[SpellScript(125883)]
public class SpellMonkZenFlightCheck : SpellScript, ISpellCheckCast
{
    public SpellCastResult CheckCast()
    {
        var player = Caster.AsPlayer;

        if (player != null)
        {
            if (player.Map.IsBattlegroundOrArena)
                return SpellCastResult.NotInBattleground;

            // In Kalimdor or Eastern Kingdom with Flight Master's License
            if (!player.HasSpell(90267) && (player.Location.MapId == 1 || player.Location.MapId == 0))
                return SpellCastResult.NotHere;

            // In Pandaria with Wisdom of the Four Winds
            if (!player.HasSpell(115913) && (player.Location.MapId == 870))
                return SpellCastResult.NotHere;

            // Legion, Broken Isles
            if (player.Location.MapId == 1220)
                return SpellCastResult.NotHere;

            // In BfA Content not yet
            if (player.Location.MapId == 1642 || player.Location.MapId == 1643)
                return SpellCastResult.NotHere;
        }

        return SpellCastResult.SpellCastOk;
    }
}