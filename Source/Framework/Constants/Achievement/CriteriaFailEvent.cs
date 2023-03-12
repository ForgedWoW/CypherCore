// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum CriteriaFailEvent : byte
{
	None = 0,
	Death = 1,                              // Death
	Hours24WithoutCompletingDailyQuest = 2, // 24 hours without completing a daily quest
	LeaveBattleground = 3,                  // Leave a battleground
	LoseRankedArenaMatchWithTeamSize = 4,   // Lose a ranked arena match with team size {#Team Size}
	LoseAura = 5,                           // Lose aura "{Spell}"
	GainAura = 6,                           // Gain aura "{Spell}"
	GainAuraEffect = 7,                     // Gain aura effect "{SpellAuraNames.EnumID}"
	CastSpell = 8,                          // Cast spell "{Spell}"
	BeSpellTarget = 9,                      // Have spell "{Spell}" cast on you
	ModifyPartyStatus = 10,                 // Modify your party status
	LosePetBattle = 11,                     // Lose a pet battle
	BattlePetDies = 12,                     // Battle pet dies
	DailyQuestsCleared = 13,                // Daily quests cleared
	SendEvent = 14,                         // Send event "{GameEvents}" (player-sent/instance only)

	Max
}