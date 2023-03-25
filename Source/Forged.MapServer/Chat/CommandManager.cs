// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Framework.Configuration;
using Framework.Database;

namespace Game.Chat;

public class CommandManager
{
	static readonly SortedDictionary<string, ChatCommandNode> _commands = new();

	public static SortedDictionary<string, ChatCommandNode> Commands => _commands;

	static CommandManager()
	{
		foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
		{
			if (type.Attributes.HasAnyFlag(TypeAttributes.NestedPrivate | TypeAttributes.NestedPublic))
				continue;

			var groupAttribute = type.GetCustomAttribute<CommandGroupAttribute>(true);

			if (groupAttribute != null)
			{
				ChatCommandNode command = new(groupAttribute);
				BuildSubCommandsForCommand(command, type);
				_commands.Add(groupAttribute.Name, command);
			}

			//This check for any command not part of that group,  but saves us from having to add them into a new class.
			foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic))
			{
				var commandAttribute = method.GetCustomAttribute<CommandNonGroupAttribute>(true);

				if (commandAttribute != null)
					_commands.Add(commandAttribute.Name, new ChatCommandNode(commandAttribute, method));
			}
		}

		var stmt = DB.World.GetPreparedStatement(WorldStatements.SEL_COMMANDS);
		var result = DB.World.Query(stmt);

		if (!result.IsEmpty())
			do
			{
				var name = result.Read<string>(0);
				var help = result.Read<string>(1);

				ChatCommandNode cmd = null;
				var map = _commands;

				foreach (var key in name.Split(' ', StringSplitOptions.RemoveEmptyEntries))
				{
					var it = map.LookupByKey(key);

					if (it != null)
					{
						cmd = it;
						map = cmd.SubCommands;
					}
					else
					{
						Log.Logger.Error($"Table `command` contains data for non-existant command '{name}'. Skipped.");
						cmd = null;

						break;
					}
				}

				if (cmd == null)
					continue;

				if (!cmd.HelpText.IsEmpty())
					Log.Logger.Error($"Table `command` contains duplicate data for command '{name}'. Skipped.");

				if (cmd.HelpString == 0)
					cmd.HelpText = help;
				else
					Log.Logger.Error($"Table `command` contains legacy help text for command '{name}', which uses `trinity_string`. Skipped.");
			} while (result.NextRow());

		foreach (var (name, cmd) in _commands)
			cmd.ResolveNames(name);
	}

	public static void InitConsole()
	{
		if (ConfigMgr.GetDefaultValue("BeepAtStart", true))
			Console.Beep();

		Console.ForegroundColor = ConsoleColor.Green;
		Console.Write("Forged>> ");

		var handler = new ConsoleHandler();

		while (!Global.WorldMgr.IsStopped)
		{
			handler.ParseCommands(Console.ReadLine());
			Console.ForegroundColor = ConsoleColor.Green;
			Console.Write("Forged>> ");
		}
	}

	static void BuildSubCommandsForCommand(ChatCommandNode command, Type type)
	{
		foreach (var nestedType in type.GetNestedTypes(BindingFlags.NonPublic))
		{
			var groupAttribute = nestedType.GetCustomAttribute<CommandGroupAttribute>(true);

			if (groupAttribute == null)
				continue;

			ChatCommandNode subCommand = new(groupAttribute);
			BuildSubCommandsForCommand(subCommand, nestedType);
			command.AddSubCommand(subCommand);
		}

		foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic))
		{
			var commandAttributes = method.GetCustomAttributes<CommandAttribute>(false).ToList();

			if (commandAttributes.Count == 0)
				continue;

			foreach (var commandAttribute in commandAttributes)
			{
				if (commandAttribute.GetType() == typeof(CommandNonGroupAttribute))
					continue;

				command.AddSubCommand(new ChatCommandNode(commandAttribute, method));
			}
		}
	}
}