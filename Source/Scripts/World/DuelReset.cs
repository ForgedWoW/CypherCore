// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IPlayer;

namespace Scripts.World.DuelReset;

[Script]
internal class DuelResetScript : ScriptObjectAutoAdd, IPlayerOnDuelStart, IPlayerOnDuelEnd
{

	public DuelResetScript() : base("DuelResetScript")
	{
	}

	// Called when a Duel ends
	public void OnDuelEnd(Player winner, Player loser, DuelCompleteType type)
	{
		// do not reset anything if DuelInterrupted or DuelFled
		if (type == DuelCompleteType.Won)
		{
			// Cooldown restore
			if (GetDefaultValue("ResetDuelCooldowns", false))
			{
				ResetSpellCooldowns(winner, false);
				ResetSpellCooldowns(loser, false);

				winner.SpellHistory.RestoreCooldownStateAfterDuel();
				loser.SpellHistory.RestoreCooldownStateAfterDuel();
			}

			// Health and mana restore
			if (GetDefaultValue("ResetDuelHealthMana", false))
			{
				winner.RestoreHealthAfterDuel();
				loser.RestoreHealthAfterDuel();

				// check if player1 class uses mana
				if (winner.DisplayPowerType == PowerType.Mana ||
					winner.Class == PlayerClass.Druid)
					winner.RestoreManaAfterDuel();

				// check if player2 class uses mana
				if (loser.DisplayPowerType == PowerType.Mana ||
					loser.Class == PlayerClass.Druid)
					loser.RestoreManaAfterDuel();
			}
		}
	}

	// Called when a Duel starts (after 3s countdown)
	public void OnDuelStart(Player player1, Player player2)
	{
		// Cooldowns reset
		if (GetDefaultValue("ResetDuelCooldowns", false))
		{
			player1.SpellHistory.SaveCooldownStateBeforeDuel();
			player2.SpellHistory.SaveCooldownStateBeforeDuel();

			ResetSpellCooldowns(player1, true);
			ResetSpellCooldowns(player2, true);
		}

		// Health and mana reset
		if (GetDefaultValue("ResetDuelHealthMana", false))
		{
			player1.SaveHealthBeforeDuel();
			player1.SaveManaBeforeDuel();
			player1.ResetAllPowers();

			player2.SaveHealthBeforeDuel();
			player2.SaveManaBeforeDuel();
			player2.ResetAllPowers();
		}
	}

	private static void ResetSpellCooldowns(Player player, bool onStartDuel)
	{
		// remove cooldowns on spells that have < 10 min Cd > 30 sec and has no onHold
		player. // remove cooldowns on spells that have < 10 min Cd > 30 sec and has no onHold
			SpellHistory
			.ResetCooldowns(pair =>
							{
								var spellInfo = Global.SpellMgr.GetSpellInfo(pair.Key);
								var remainingCooldown = player.SpellHistory.GetRemainingCooldown(spellInfo);
								var totalCooldown = TimeSpan.FromMilliseconds(spellInfo.RecoveryTime);
								var categoryCooldown = TimeSpan.FromMilliseconds(spellInfo.CategoryRecoveryTime);

								void applySpellMod(ref TimeSpan value)
								{
									var intValue = (int)value.TotalMilliseconds;
									player.ApplySpellMod(spellInfo, SpellModOp.Cooldown, ref intValue, null);
									value = TimeSpan.FromMilliseconds(intValue);
								}

								;

								applySpellMod(ref totalCooldown);

								var cooldownMod = player.GetTotalAuraModifier(AuraType.ModCooldown);

								if (cooldownMod != 0)
									totalCooldown += TimeSpan.FromMilliseconds(cooldownMod);

								if (!spellInfo.HasAttribute(SpellAttr6.NoCategoryCooldownMods))
									applySpellMod(ref categoryCooldown);

								return remainingCooldown > TimeSpan.Zero && !pair.Value.OnHold && totalCooldown < TimeSpan.FromMinutes(10) && categoryCooldown < TimeSpan.FromMinutes(10) && remainingCooldown < TimeSpan.FromMinutes(10) && (onStartDuel ? totalCooldown - remainingCooldown > TimeSpan.FromSeconds(30) : true) && (onStartDuel ? categoryCooldown - remainingCooldown > TimeSpan.FromSeconds(30) : true);
							},
							true);

		// pet cooldowns
		var pet = player.CurrentPet;

		if (pet)
			pet.SpellHistory.ResetAllCooldowns();
	}
}