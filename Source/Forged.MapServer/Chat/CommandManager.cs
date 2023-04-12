// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Framework.Database;
using Framework.Util;
using Game.Common.Extendability;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Chat;

public class CommandManager
{
    private readonly IConfiguration _configuration;

    public SortedDictionary<string, ChatCommandNode> Commands { get; } = new();
    public bool Running { get; set; }

    public CommandManager(WorldDatabase worldDatabase, IConfiguration configuration)
    {
        _configuration = configuration;

        foreach (var ass in IOHelpers.GetAllAssembliesInDir(configuration.GetDefaultValue("ScriptsDirectory", Path.Combine(AppContext.BaseDirectory, "Scripts"))))
            foreach (var type in ass.GetTypes())
            {
                if (type.Attributes.HasAnyFlag(TypeAttributes.NestedPrivate | TypeAttributes.NestedPublic))
                    continue;

                var groupAttribute = type.GetCustomAttribute<CommandGroupAttribute>(true);

                if (groupAttribute != null)
                {
                    ChatCommandNode command = new(groupAttribute);
                    BuildSubCommandsForCommand(command, type);
                    Commands.Add(groupAttribute.Name, command);
                }

                //This check for any command not part of that group,  but saves us from having to add them into a new class.
                foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic))
                {
                    var commandAttribute = method.GetCustomAttribute<CommandNonGroupAttribute>(true);

                    if (commandAttribute != null)
                        Commands.Add(commandAttribute.Name, new ChatCommandNode(commandAttribute, method));
                }
            }

        var stmt = worldDatabase.GetPreparedStatement(WorldStatements.SEL_COMMANDS);
        var result = worldDatabase.Query(stmt);

        if (!result.IsEmpty())
            do
            {
                var name = result.Read<string>(0);
                var help = result.Read<string>(1);

                ChatCommandNode cmd = null;
                var map = Commands;

                foreach (var key in name.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (map.TryGetValue(key, out var it))
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

        foreach (var (name, cmd) in Commands)
            cmd.ResolveNames(name);
    }

    public void InitConsole()
    {
        if (_configuration.GetDefaultValue("BeepAtStart", true))
            Console.Beep();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("Forged>> ");

        var handler = new ConsoleHandler();
        Running = true;

        while (Running)
        {
            handler.ParseCommands(Console.ReadLine());
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Forged>> ");
        }
    }

    private void BuildSubCommandsForCommand(ChatCommandNode command, Type type)
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

            foreach (var commandAttribute in commandAttributes.Where(commandAttribute => commandAttribute.GetType() != typeof(CommandNonGroupAttribute)))
                command.AddSubCommand(new ChatCommandNode(commandAttribute, method));
        }
    }
}