// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Entities.Players;

namespace Forged.RealmServer.Scripting.Interfaces.IQuest;

public interface IQuestOnQuestStatusChange : IScriptObject
{
	void OnQuestStatusChange(Player player, Quest quest, QuestStatus oldStatus, QuestStatus newStatus);
}