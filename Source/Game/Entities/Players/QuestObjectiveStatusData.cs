// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Entities;

struct QuestObjectiveStatusData
{
	public (uint QuestID, QuestStatusData Status) QuestStatusPair;
	public QuestObjective Objective;
}