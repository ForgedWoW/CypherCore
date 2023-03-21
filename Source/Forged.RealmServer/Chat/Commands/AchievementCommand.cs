// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.DataStorage;

namespace Forged.RealmServer.Chat.Commands;

[CommandGroup("achievement")]
class AchievementCommand
{
	[Command("add", CypherStrings.CommandAchievementAddHelp, RBACPermissions.CommandAchievementAdd)]
	static bool HandleAchievementAddCommand(CommandHandler handler, AchievementRecord achievementEntry)
	{
		var target = handler.SelectedPlayer;

		if (!target)
		{
			handler.SendSysMessage(CypherStrings.NoCharSelected);

			return false;
		}

		target.CompletedAchievement(achievementEntry);

		return true;
	}
}