// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.RealmServer.Chat;

[AttributeUsage(AttributeTargets.Method)]
public class CommandNonGroupAttribute : CommandAttribute
{
	public CommandNonGroupAttribute(string command, CypherStrings help, RBACPermissions rbac, bool allowConsole = false) : base(command, help, rbac, allowConsole) { }
	public CommandNonGroupAttribute(string command, RBACPermissions rbac, bool allowConsole = false) : base(command, rbac, allowConsole) { }
}