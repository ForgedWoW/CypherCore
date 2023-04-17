// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

[Script]
public class RageOnTakeDamage : ScriptObjectAutoAddDBBound, IPlayerOnTakeDamage
{
    public RageOnTakeDamage() : base("rage_on_take_damage") { }
    public PlayerClass PlayerClass => PlayerClass.Warrior;

    public void OnPlayerTakeDamage(Player player, double amount, SpellSchoolMask schoolMask)
    {
        var rage = player.GetPower(PowerType.Rage);
        var mod = 30;
        player.SetPower(PowerType.Rage, rage + mod);
    }
}