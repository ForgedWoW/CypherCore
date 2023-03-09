// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Framework.Constants;
using Framework.IO;
using Game.DataStorage;

namespace Game.Chat;

public class ChatCommandNode
{
	public readonly SortedDictionary<string, ChatCommandNode> SubCommands = new();
	public string Name;
	public CommandPermissions Permission;
	public string HelpText;
	public CypherStrings HelpString;

	MethodInfo _methodInfo;
	ParameterInfo[] _parameters;

	public ChatCommandNode(CommandAttribute attribute)
	{
		Name = attribute.Name;
		Permission = new CommandPermissions(attribute.RBAC, attribute.AllowConsole);
		HelpString = attribute.Help;
	}

	public ChatCommandNode(CommandAttribute attribute, MethodInfo methodInfo) : this(attribute)
	{
		_methodInfo = methodInfo;
		_parameters = methodInfo.GetParameters();
	}

	public static bool TryExecuteCommand(CommandHandler handler, string cmdStr)
	{
		ChatCommandNode cmd = null;
		var map = CommandManager.Commands;

		cmdStr = cmdStr.Trim(' ');

		var oldTail = cmdStr;

		while (!oldTail.IsEmpty())
		{
			/* oldTail = token DELIMITER newTail */
			var (token, newTail) = oldTail.Tokenize();
			Cypher.Assert(!token.IsEmpty());
			var listOfPossibleCommands = map.Where(p => p.Key.StartsWith(token) && p.Value.IsVisible(handler)).ToList();

			if (listOfPossibleCommands.Empty())
				break; /* no matching subcommands found */

			if (!listOfPossibleCommands[0].Key.Equals(token, StringComparison.OrdinalIgnoreCase))
				/* ok, so it1 points at a partially matching subcommand - let's see if there are others */
				if (listOfPossibleCommands.Count > 1)
				{
					/* there are multiple matching subcommands - print possibilities and return */
					if (cmd != null)
						handler.SendSysMessage(CypherStrings.SubcmdAmbiguous, cmd.Name, ' ', token);
					else
						handler.SendSysMessage(CypherStrings.CmdAmbiguous, token);

					handler.SendSysMessage(listOfPossibleCommands[0].Value.HasVisibleSubCommands(handler) ? CypherStrings.SubcmdsListEntryEllipsis : CypherStrings.SubcmdsListEntry, listOfPossibleCommands[0].Key);

					foreach (var (name, command) in listOfPossibleCommands)
						handler.SendSysMessage(command.HasVisibleSubCommands(handler) ? CypherStrings.SubcmdsListEntryEllipsis : CypherStrings.SubcmdsListEntry, name);

					return true;
				}

			/* now we matched exactly one subcommand, and it1 points to it; go down the rabbit hole */
			cmd = listOfPossibleCommands[0].Value;
			map = cmd.SubCommands;

			oldTail = newTail;
		}

		if (cmd != null)
		{
			/* if we matched a command at some point, invoke it */
			handler.SetSentErrorMessage(false);

			if (cmd.IsInvokerVisible(handler) && cmd.Invoke(handler, oldTail))
			{
				/* invocation succeeded, log this */
				if (!handler.IsConsole)
					LogCommandUsage(handler.Session, (uint)cmd.Permission.RequiredPermission, cmdStr);
			}
			else if (!handler.HasSentErrorMessage)
			{
				/* invocation failed, we should show usage */
				cmd.SendCommandHelp(handler);
			}

			return true;
		}

		return false;
	}

	public static void SendCommandHelpFor(CommandHandler handler, string cmdStr)
	{
		ChatCommandNode cmd = null;
		var map = CommandManager.Commands;

		foreach (var token in cmdStr.Split(' ', StringSplitOptions.RemoveEmptyEntries))
		{
			var listOfPossibleCommands = map.Where(p => p.Key.StartsWith(token) && p.Value.IsVisible(handler)).ToList();

			if (listOfPossibleCommands.Empty())
			{
				/* no matching subcommands found */
				if (cmd != null)
				{
					cmd.SendCommandHelp(handler);
					handler.SendSysMessage(CypherStrings.SubcmdInvalid, cmd.Name, ' ', token);
				}
				else
				{
					handler.SendSysMessage(CypherStrings.CmdInvalid, token);
				}

				return;
			}

			if (!listOfPossibleCommands[0].Key.Equals(token, StringComparison.OrdinalIgnoreCase))
				/* ok, so it1 points at a partially matching subcommand - let's see if there are others */
				if (listOfPossibleCommands.Count > 1)
				{
					/* there are multiple matching subcommands - print possibilities and return */
					if (cmd != null)
						handler.SendSysMessage(CypherStrings.SubcmdAmbiguous, cmd.Name, ' ', token);
					else
						handler.SendSysMessage(CypherStrings.CmdAmbiguous, token);

					handler.SendSysMessage(listOfPossibleCommands[0].Value.HasVisibleSubCommands(handler) ? CypherStrings.SubcmdsListEntryEllipsis : CypherStrings.SubcmdsListEntry, listOfPossibleCommands[0].Key);

					foreach (var (name, command) in listOfPossibleCommands)
						handler.SendSysMessage(command.HasVisibleSubCommands(handler) ? CypherStrings.SubcmdsListEntryEllipsis : CypherStrings.SubcmdsListEntry, name);

					return;
				}

			cmd = listOfPossibleCommands[0].Value;
			map = cmd.SubCommands;
		}

		if (cmd != null)
		{
			cmd.SendCommandHelp(handler);
		}
		else if (cmdStr.IsEmpty())
		{
			handler.SendSysMessage(CypherStrings.AvailableCmds);

			foreach (var (name, command) in map)
			{
				if (!command.IsVisible(handler))
					continue;

				handler.SendSysMessage(command.HasVisibleSubCommands(handler) ? CypherStrings.SubcmdsListEntryEllipsis : CypherStrings.SubcmdsListEntry, name);
			}
		}
		else
		{
			handler.SendSysMessage(CypherStrings.CmdInvalid, cmdStr);
		}
	}

	public void ResolveNames(string name)
	{
		if (_methodInfo != null && (HelpText.IsEmpty() && HelpString == 0))
			Log.outWarn(LogFilter.Sql, $"Table `command` is missing help text for command '{name}'.");

		Name = name;

		foreach (var (subToken, cmd) in SubCommands)
			cmd.ResolveNames($"{name} {subToken}");
	}

	public void SendCommandHelp(CommandHandler handler)
	{
		var hasInvoker = IsInvokerVisible(handler);

		if (hasInvoker)
		{
			if (HelpString != 0)
			{
				handler.SendSysMessage(HelpString);
			}
			else if (!HelpText.IsEmpty())
			{
				handler.SendSysMessage(HelpText);
			}
			else
			{
				handler.SendSysMessage(CypherStrings.CmdHelpGeneric, Name);
				handler.SendSysMessage(CypherStrings.CmdNoHelpAvailable, Name);
			}
		}

		var header = false;

		foreach (var (_, command) in SubCommands)
		{
			var subCommandHasSubCommand = command.HasVisibleSubCommands(handler);

			if (!subCommandHasSubCommand && !command.IsInvokerVisible(handler))
				continue;

			if (!header)
			{
				if (!hasInvoker)
					handler.SendSysMessage(CypherStrings.CmdHelpGeneric, Name);

				handler.SendSysMessage(CypherStrings.SubcmdsList);
				header = true;
			}

			handler.SendSysMessage(subCommandHasSubCommand ? CypherStrings.SubcmdsListEntryEllipsis : CypherStrings.SubcmdsListEntry, command.Name);
		}
	}

	public void AddSubCommand(ChatCommandNode command)
	{
		if (command.Name.IsEmpty())
		{
			Permission = command.Permission;
			HelpText = command.HelpText;
			HelpString = command.HelpString;
			_methodInfo = command._methodInfo;
			_parameters = command._parameters;
		}
		else
		{
			if (!SubCommands.TryAdd(command.Name, command))
				Log.outError(LogFilter.Commands, $"Error trying to add subcommand, Already exists Command: {Name} SubCommand: {command.Name}");
		}
	}

	public bool Invoke(CommandHandler handler, string args)
	{
		if (_parameters.Any(p => p.ParameterType == typeof(StringArguments))) //Old system, can remove once all commands are changed.
		{
			return (bool)_methodInfo.Invoke(null,
											new object[]
											{
												handler, new StringArguments(args)
											});
		}
		else
		{
			var parseArgs = new dynamic[_parameters.Length];
			parseArgs[0] = handler;
			var result = CommandArgs.ConsumeFromOffset(parseArgs, 1, _parameters, handler, args);

			if (result.IsSuccessful)
			{
				return (bool)_methodInfo.Invoke(null, parseArgs);
			}
			else
			{
				if (result.HasErrorMessage)
				{
					handler.SendSysMessage(result.ErrorMessage);
					handler.SetSentErrorMessage(true);
				}

				return false;
			}
		}
	}

	bool IsInvokerVisible(CommandHandler who)
	{
		if (_methodInfo == null)
			return false;

		if (who.IsConsole && !Permission.AllowConsole)
			return false;

		return who.HasPermission(Permission.RequiredPermission);
	}

	bool HasVisibleSubCommands(CommandHandler who)
	{
		foreach (var (_, command) in SubCommands)
			if (command.IsVisible(who))
				return true;

		return false;
	}

	static void LogCommandUsage(WorldSession session, uint permission, string cmdStr)
	{
		if (Global.AccountMgr.IsPlayerAccount(session.Security))
			return;

		if (Global.AccountMgr.GetRBACPermission((uint)RBACPermissions.RolePlayer).GetLinkedPermissions().Contains(permission))
			return;

		var player = session.Player;
		var targetGuid = player.Target;
		var areaId = player.Area;
		var areaName = "Unknown";
		var zoneName = "Unknown";

		var area = CliDB.AreaTableStorage.LookupByKey(areaId);

		if (area != null)
		{
			var locale = session.SessionDbcLocale;
			areaName = area.AreaName[locale];
			var zone = CliDB.AreaTableStorage.LookupByKey(area.ParentAreaID);

			if (zone != null)
				zoneName = zone.AreaName[locale];
		}

		Log.outCommand(session.AccountId,
						$"Command: {cmdStr} [Player: {player.GetName()} ({player.GUID}) (Account: {session.AccountId}) " +
						$"X: {player.Location.X} Y: {player.Location.Y} Z: {player.Location.Z} Map: {player.Location.MapId} ({(player.Map ? player.Map.MapName : "Unknown")}) " +
						$"Area: {areaId} ({areaName}) Zone: {zoneName} Selected: {(player.SelectedUnit ? player.SelectedUnit.GetName() : "")} ({targetGuid})]");
	}

	bool IsVisible(CommandHandler who)
	{
		return IsInvokerVisible(who) || HasVisibleSubCommands(who);
	}
}