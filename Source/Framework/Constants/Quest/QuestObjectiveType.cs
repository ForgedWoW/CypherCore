// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum QuestObjectiveType
{
	Monster = 0,
	Item = 1,
	GameObject = 2,
	TalkTo = 3,
	Currency = 4,
	LearnSpell = 5,
	MinReputation = 6,
	MaxReputation = 7,
	Money = 8,
	PlayerKills = 9,
	AreaTrigger = 10,
	WinPetBattleAgainstNpc = 11,
	DefeatBattlePet = 12,
	WinPvpPetBattles = 13,
	CriteriaTree = 14,
	ProgressBar = 15,
	HaveCurrency = 16,       // requires the player to have X currency when turning in but does not consume it
	ObtainCurrency = 17,     // requires the player to gain X currency after starting the quest but not required to keep it until the end (does not consume)
	IncreaseReputation = 18, // requires the player to gain X reputation with a faction
	AreaTriggerEnter = 19,
	AreaTriggerExit = 20,
	Max
}