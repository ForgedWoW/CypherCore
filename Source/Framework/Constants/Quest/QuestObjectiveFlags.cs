// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum QuestObjectiveFlags
{
	TrackedOnMinimap = 0x01, // Client Displays Large Yellow Blob On Minimap For Creature/Gameobject
	Sequenced = 0x02,        // Client Will Not See The Objective Displayed Until All Previous Objectives Are Completed
	Optional = 0x04,         // Not Required To Complete The Quest
	Hidden = 0x08,           // Never Displayed In Quest Log
	HideCreditMsg = 0x10,    // Skip Showing Item Objective Progress
	PreserveQuestItems = 0x20,
	PartOfProgressBar = 0x40, // Hidden Objective Used To Calculate Progress Bar Percent (Quests Are Limited To A Single Progress Bar Objective)
	KillPlayersSameFaction = 0x80
}