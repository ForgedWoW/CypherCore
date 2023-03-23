// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.DataStorage;
using Game.Common.Chat;
using Game.Common.DataStorage.Structs.A;

namespace Game.Common.Chat.Commands;

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
