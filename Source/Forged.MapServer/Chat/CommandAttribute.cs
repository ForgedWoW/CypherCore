// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Chat;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class CommandAttribute : Attribute
{
    public CommandAttribute(string command)
    {
        Name = command.ToLower();
    }

    public CommandAttribute(string command, RBACPermissions rbac, bool allowConsole = false)
    {
        Name = command.ToLower();
        RBAC = rbac;
        AllowConsole = allowConsole;
    }

    public CommandAttribute(string command, CypherStrings help, RBACPermissions rbac, bool allowConsole = false)
    {
        Name = command.ToLower();
        Help = help;
        RBAC = rbac;
        AllowConsole = allowConsole;
    }

    /// <summary>
    ///     Allow Console?
    /// </summary>
    public bool AllowConsole { get; private set; }

    /// <summary>
    ///     Help String for command.
    /// </summary>
    public CypherStrings Help { get; set; }

    /// <summary>
    ///     Command's name.
    /// </summary>
    public string Name { get; private set; }
	/// <summary>
	///     Minimum user level required to invoke the command.
	/// </summary>
	public RBACPermissions RBAC { get; set; }
}