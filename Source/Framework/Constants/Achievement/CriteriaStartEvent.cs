// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum CriteriaStartEvent : byte
{
	None = 0,                            // - NONE -
	ReachLevel = 1,                      // Reach level {#Level}
	CompleteDailyQuest = 2,              // Complete daily quest "{QuestV2}"
	StartBattleground = 3,               // Start battleground "{Map}"
	WinRankedArenaMatchWithTeamSize = 4, // Win a ranked arena match with team size {#Team Size}
	GainAura = 5,                        // Gain aura "{Spell}"
	GainAuraEffect = 6,                  // Gain aura effect "{SpellAuraNames.EnumID}"
	CastSpell = 7,                       // Cast spell "{Spell}"
	BeSpellTarget = 8,                   // Have spell "{Spell}" cast on you
	AcceptQuest = 9,                     // Accept quest "{QuestV2}"
	KillNPC = 10,                        // Kill NPC "{Creature}"
	KillPlayer = 11,                     // Kill player
	UseItem = 12,                        // Use item "{Item}"
	SendEvent = 13,                      // Send event "{GameEvents}" (player-sent/instance only)
	BeginScenarioStep = 14,              // Begin scenario step "{#Step}" (for use with "Player on Scenario" modifier only)

	Max
}