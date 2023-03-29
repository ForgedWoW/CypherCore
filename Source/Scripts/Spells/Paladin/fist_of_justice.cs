// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IPlayer;

namespace Scripts.Spells.Paladin;

//234299
[Script]
public class fist_of_justice : ScriptObjectAutoAdd, IPlayerOnModifyPower
{
    public PlayerClass PlayerClass { get; } = PlayerClass.Paladin;

    public fist_of_justice() : base("fist_of_justice") { }

    public void OnModifyPower(Player player, PowerType power, int oldValue, ref int newValue, bool regen)
    {
        if (player.Class != PlayerClass.Paladin)
            return;

        if (!player.HasAura(PaladinSpells.FIST_OF_JUSTICE))
            return;

        if (player.DisplayPowerType == PowerType.HolyPower)
            if (newValue < oldValue)
                if (player.HasAura(PaladinSpells.FIST_OF_JUSTICE))
                    player.SpellHistory.ModifyCooldown(PaladinSpells.HammerOfJustice, TimeSpan.FromSeconds(-2));
    }
}