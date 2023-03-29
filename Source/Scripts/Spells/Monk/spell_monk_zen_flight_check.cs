// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(125883)]
public class spell_monk_zen_flight_check : SpellScript, ISpellCheckCast
{
    public SpellCastResult CheckCast()
    {
        var _player = Caster.AsPlayer;

        if (_player != null)
        {
            if (_player.Map.IsBattlegroundOrArena)
                return SpellCastResult.NotInBattleground;

            // In Kalimdor or Eastern Kingdom with Flight Master's License
            if (!_player.HasSpell(90267) && (_player.Location.MapId == 1 || _player.Location.MapId == 0))
                return SpellCastResult.NotHere;

            // In Pandaria with Wisdom of the Four Winds
            if (!_player.HasSpell(115913) && (_player.Location.MapId == 870))
                return SpellCastResult.NotHere;

            // Legion, Broken Isles
            if (_player.Location.MapId == 1220)
                return SpellCastResult.NotHere;

            // In BfA Content not yet
            if (_player.Location.MapId == 1642 || _player.Location.MapId == 1643)
                return SpellCastResult.NotHere;
        }

        return SpellCastResult.SpellCastOk;
    }
}