// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Framework.Constants;

namespace Scripts.Spells.Paladin;

//234299
[Script]
public class FistOfJustice : ScriptObjectAutoAdd, IPlayerOnModifyPower
{
    public FistOfJustice() : base("fist_of_justice") { }
    public PlayerClass PlayerClass { get; } = PlayerClass.Paladin;

    public void OnModifyPower(Player player, PowerType power, int oldValue, ref int newValue, bool regen)
    {
        if (player.Class != PlayerClass.Paladin)
            return;

        if (!player.HasAura(PaladinSpells.FIST_OF_JUSTICE))
            return;

        if (player.DisplayPowerType == PowerType.HolyPower)
            if (newValue < oldValue)
                if (player.HasAura(PaladinSpells.FIST_OF_JUSTICE))
                    player.SpellHistory.ModifyCooldown(PaladinSpells.HAMMER_OF_JUSTICE, TimeSpan.FromSeconds(-2));
    }
}