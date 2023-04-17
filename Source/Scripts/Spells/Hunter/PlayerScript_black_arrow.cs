// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IPlayer;

namespace Scripts.Spells.Hunter;

[Script]
public class PlayerScriptBlackArrow : ScriptObjectAutoAdd, IPlayerOnCreatureKill, IPlayerOnPVPKill
{
    public PlayerScriptBlackArrow() : base("PlayerScript_black_arrow") { }

    public void OnCreatureKill(Player player, Creature unnamedParameter)
    {
        if (player.HasSpell(HunterSpells.BLACK_ARROW))
            if (player.SpellHistory.HasCooldown(HunterSpells.BLACK_ARROW))
                player.SpellHistory.ResetCooldown(HunterSpells.BLACK_ARROW, true);
    }

    public void OnPVPKill(Player killer, Player unnamedParameter)
    {
        if (killer.HasSpell(HunterSpells.BLACK_ARROW))
            if (killer.SpellHistory.HasCooldown(HunterSpells.BLACK_ARROW))
                killer.SpellHistory.ResetCooldown(HunterSpells.BLACK_ARROW, true);
    }
}