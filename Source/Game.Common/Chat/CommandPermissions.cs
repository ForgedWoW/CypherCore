// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Chat;

public struct CommandPermissions
{
	public RBACPermissions RequiredPermission;
	public bool AllowConsole;

	public CommandPermissions(RBACPermissions perm, bool allowConsole)
	{
		RequiredPermission = perm;
		AllowConsole = allowConsole;
	}
}