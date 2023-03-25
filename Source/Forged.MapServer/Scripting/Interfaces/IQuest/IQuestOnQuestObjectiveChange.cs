// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Quest;

namespace Forged.MapServer.Scripting.Interfaces.IQuest;

public interface IQuestOnQuestObjectiveChange : IScriptObject
{
	void OnQuestObjectiveChange(Player player, Quest.Quest quest, QuestObjective objective, int oldAmount, int newAmount);
}