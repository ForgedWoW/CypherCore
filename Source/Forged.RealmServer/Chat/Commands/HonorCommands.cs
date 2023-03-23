// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;

namespace Forged.RealmServer.Chat.Commands;

[CommandGroup("honor")]
class HonorCommands
{
	[Command("update", RBACPermissions.CommandHonorUpdate)]
	static bool HandleHonorUpdateCommand(CommandHandler handler)
	{
		var target = handler.SelectedPlayer;

		if (!target)
		{
			handler.SendSysMessage(CypherStrings.PlayerNotFound);

			return false;
		}

		// check online security
		if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
			return false;

		target.UpdateHonorFields();

		return true;
	}

	[CommandGroup("add")]
	class HonorAddCommands
	{
		[Command("", RBACPermissions.CommandHonorAdd)]
		static bool HandleHonorAddCommand(CommandHandler handler, int amount)
		{
			var target = handler.SelectedPlayer;

			if (!target)
			{
				handler.SendSysMessage(CypherStrings.PlayerNotFound);

				return false;
			}

			// check online security
			if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
				return false;

			target.RewardHonor(null, 1, amount);

			return true;
		}

		[Command("kill", RBACPermissions.CommandHonorAddKill)]
		static bool HandleHonorAddKillCommand(CommandHandler handler)
		{
			var target = handler.SelectedUnit;

			if (!target)
			{
				handler.SendSysMessage(CypherStrings.PlayerNotFound);

				return false;
			}

			// check online security
			var player = target.AsPlayer;

			if (player)
				if (handler.HasLowerSecurity(player, ObjectGuid.Empty))
					return false;

			handler.Player.RewardHonor(target, 1);

			return true;
		}
	}
}