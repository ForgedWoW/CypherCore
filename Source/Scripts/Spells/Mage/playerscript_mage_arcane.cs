﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IPlayer;

namespace Scripts.Spells.Mage;

[Script]
public class playerscript_mage_arcane : ScriptObjectAutoAdd, IPlayerOnAfterModifyPower
{
	public PlayerClass PlayerClass { get; } = PlayerClass.Warlock;

	public playerscript_mage_arcane() : base("playerscript_mage_arcane") { }

	public void OnAfterModifyPower(Player player, PowerType power, int oldValue, int newValue, bool regen)
	{
		if (power != PowerType.ArcaneCharges)
			return;

		// Going up in charges is handled by aura 190427
		// Decreasing power seems weird clientside does not always match serverside power amount (client stays at 1, server is at 0)
		if (newValue != 0)
		{
			var arcaneCharge = player.GetAura(MageSpells.ARCANE_CHARGE);

			if (arcaneCharge != null)
				arcaneCharge.SetStackAmount((byte)newValue);
		}
		else
		{
			player.RemoveAura(MageSpells.ARCANE_CHARGE);
		}

		if (player.HasAura(MageSpells.RULE_OF_THREES))
			if (newValue == 3 && oldValue == 2)
				player.CastSpell(player, MageSpells.RULE_OF_THREES_BUFF, true);
	}
}