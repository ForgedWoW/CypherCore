// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Chrono;
using Forged.MapServer.Server;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.IO;
using Framework.Util;

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("server")]
internal class ServerCommands
{
    [Command("corpses", RBACPermissions.CommandServerCorpses, true)]
    private static bool HandleServerCorpsesCommand(CommandHandler handler)
    {
        handler.WorldManager.RemoveOldCorpses();

        return true;
    }

    [Command("debug", RBACPermissions.CommandServerCorpses, true)]
    private static bool HandleServerDebugCommand(CommandHandler handler)
    {
        return false; //todo fix me
    }

    [Command("exit", RBACPermissions.CommandServerExit, true)]
    private static bool HandleServerExitCommand(CommandHandler handler)
    {
        handler.SendSysMessage(CypherStrings.CommandExit);
        handler.WorldManager.StopNow(ShutdownExitCode.Shutdown);

        return true;
    }

    [Command("info", RBACPermissions.CommandServerInfo, true)]
    private static bool HandleServerInfoCommand(CommandHandler handler)
    {
        var playersNum = handler.WorldManager.PlayerCount;
        var maxPlayersNum = handler.WorldManager.MaxPlayerCount;
        var activeClientsNum = handler.WorldManager.ActiveSessionCount;
        var queuedClientsNum = handler.WorldManager.QueuedSessionCount;
        var maxActiveClientsNum = handler.WorldManager.MaxActiveSessionCount;
        var maxQueuedClientsNum = handler.WorldManager.MaxQueuedSessionCount;
        var uptime = Time.SecsToTimeString((uint)GameTime.Uptime);
        var updateTime = handler.WorldManager.WorldUpdateTime.GetLastUpdateTime();

        handler.SendSysMessage(CypherStrings.ConnectedPlayers, playersNum, maxPlayersNum);
        handler.SendSysMessage(CypherStrings.ConnectedUsers, activeClientsNum, maxActiveClientsNum, queuedClientsNum, maxQueuedClientsNum);
        handler.SendSysMessage(CypherStrings.Uptime, uptime);
        handler.SendSysMessage(CypherStrings.UpdateDiff, updateTime);

        // Can't use handler.WorldManager.ShutdownMsg here in case of console command
        if (handler.WorldManager.IsShuttingDown)
            handler.SendSysMessage(CypherStrings.ShutdownTimeleft, Time.SecsToTimeString(handler.WorldManager.ShutDownTimeLeft));

        return true;
    }

    [Command("motd", RBACPermissions.CommandServerMotd, true)]
    private static bool HandleServerMotdCommand(CommandHandler handler)
    {
        var motd = "";

        foreach (var line in handler.WorldManager.Motd)
            motd += line;

        handler.SendSysMessage(CypherStrings.MotdCurrent, motd);

        return true;
    }

    [Command("plimit", RBACPermissions.CommandServerPlimit, true)]
    private static bool HandleServerPLimitCommand(CommandHandler handler, StringArguments args)
    {
        if (!args.Empty())
        {
            var paramStr = args.NextString();

            if (string.IsNullOrEmpty(paramStr))
                return false;

            switch (paramStr.ToLower())
            {
                case "player":
                    handler.WorldManager.PlayerSecurityLimit = AccountTypes.Player;

                    break;
                case "moderator":
                    handler.WorldManager.PlayerSecurityLimit = AccountTypes.Moderator;

                    break;
                case "gamemaster":
                    handler.WorldManager.PlayerSecurityLimit = AccountTypes.GameMaster;

                    break;
                case "administrator":
                    handler.WorldManager.PlayerSecurityLimit = AccountTypes.Administrator;

                    break;
                case "reset":
                    handler.WorldManager.PlayerAmountLimit = handler.Configuration.GetDefaultValue<uint>("PlayerLimit", 100);
                    handler.WorldManager.LoadDBAllowedSecurityLevel();

                    break;
                default:
                    if (!int.TryParse(paramStr, out var value))
                        return false;

                    if (value < 0)
                        handler.WorldManager.PlayerSecurityLimit = (AccountTypes)(-value);
                    else
                        handler.WorldManager.PlayerAmountLimit = (uint)value;

                    break;
            }
        }

        var playerAmountLimit = handler.WorldManager.PlayerAmountLimit;
        var allowedAccountType = handler.WorldManager.PlayerSecurityLimit;

        string secName = allowedAccountType switch
        {
            AccountTypes.Player        => "Player",
            AccountTypes.Moderator     => "Moderator",
            AccountTypes.GameMaster    => "Gamemaster",
            AccountTypes.Administrator => "Administrator",
            _                          => "<unknown>"
        };

        handler.SendSysMessage("Player limits: amount {0}, min. security level {1}.", playerAmountLimit, secName);

        return true;
    }

    private static bool IsOnlyUser(WorldSession mySession, CommandHandler handler)
    {
        // check if there is any session connected from a different address
        var myAddr = mySession ? mySession.RemoteAddress : "";
        var sessions = handler.WorldManager.AllSessions;

        foreach (var session in sessions)
            if (session && myAddr != session.RemoteAddress)
                return false;

        return true;
    }

    private static bool ParseExitCode(string exitCodeStr, out int exitCode)
    {
        if (!int.TryParse(exitCodeStr, out exitCode))
            return false;

        return exitCode switch
        {
            // Handle atoi() errors
            0 when exitCodeStr[0] != '0' || (exitCodeStr.Length > 1 && exitCodeStr[1] != '\0') => false,
            // Exit code should be in range of 0-125, 126-255 is used
            // in many shells for their own return codes and code > 255
            // is not supported in many others
            < 0 or > 125 => false,
            _            => true
        };
    }

    private static bool ShutdownServer(StringArguments args, CommandHandler handler, ShutdownMask shutdownMask, ShutdownExitCode defaultExitCode)
    {
        if (args.Empty())
            return false;

        var delayStr = args.NextString();

        if (delayStr.IsEmpty())
            return false;

        if (int.TryParse(delayStr, out var delay))
        {
            //  Prevent interpret wrong arg value as 0 secs shutdown time
            if ((delay == 0 && (delayStr[0] != '0' || delayStr.Length > 1 && delayStr[1] != '\0')) || delay < 0)
                return false;
        }
        else
        {
            delay = (int)Time.TimeStringToSecs(delayStr);

            if (delay == 0)
                return false;
        }

        var reason = "";
        var exitCodeStr = "";
        string nextToken;

        while (!(nextToken = args.NextString()).IsEmpty())
            if (nextToken.IsNumber())
            {
                exitCodeStr = nextToken;
            }
            else
            {
                reason = nextToken;
                reason += args.NextString("\0");

                break;
            }

        var exitCode = (int)defaultExitCode;

        if (!exitCodeStr.IsEmpty())
            if (!ParseExitCode(exitCodeStr, out exitCode))
                return false;

        // Override parameter "delay" with the configuration value if there are still players connected and "force" parameter was not specified
        if (delay < handler.Configuration.GetDefaultValue("GM:ForceShutdownThreshold", 30) && !shutdownMask.HasAnyFlag(ShutdownMask.Force) && !IsOnlyUser(handler.Session, handler))
        {
            delay = handler.Configuration.GetDefaultValue("GM:ForceShutdownThreshold", 30);
            handler.SendSysMessage(CypherStrings.ShutdownDelayed, delay);
        }

        handler.WorldManager.ShutdownServ((uint)delay, shutdownMask, (ShutdownExitCode)exitCode, reason);

        return true;
    }

    [CommandGroup("idleRestart")]
    private class IdleRestartCommands
    {
        [Command("", RBACPermissions.CommandServerIdlerestart, true)]
        private static bool HandleServerIdleRestartCommand(CommandHandler handler, StringArguments args)
        {
            return ShutdownServer(args, handler, ShutdownMask.Restart | ShutdownMask.Idle, ShutdownExitCode.Restart);
        }

        [Command("cancel", RBACPermissions.CommandServerIdlerestartCancel, true)]
        private static bool HandleServerShutDownCancelCommand(CommandHandler handler)
        {
            var timer = handler.WorldManager.ShutdownCancel();

            if (timer != 0)
                handler.SendSysMessage(CypherStrings.ShutdownCancelled, timer);

            return true;
        }
    }

    [CommandGroup("idleshutdown")]
    private class IdleshutdownCommands
    {
        [Command("", RBACPermissions.CommandServerIdleshutdown, true)]
        private static bool HandleServerIdleShutDownCommand(CommandHandler handler, StringArguments args)
        {
            return ShutdownServer(args, handler, ShutdownMask.Idle, ShutdownExitCode.Shutdown);
        }

        [Command("cancel", RBACPermissions.CommandServerIdleshutdownCancel, true)]
        private static bool HandleServerShutDownCancelCommand(CommandHandler handler)
        {
            var timer = handler.WorldManager.ShutdownCancel();

            if (timer != 0)
                handler.SendSysMessage(CypherStrings.ShutdownCancelled, timer);

            return true;
        }
    }

    [CommandGroup("restart")]
    private class RestartCommands
    {
        [Command("force", RBACPermissions.CommandServerRestartCancel, true)]
        private static bool HandleServerForceRestartCommand(CommandHandler handler, StringArguments args)
        {
            return ShutdownServer(args, handler, ShutdownMask.Force | ShutdownMask.Restart, ShutdownExitCode.Restart);
        }

        [Command("", RBACPermissions.CommandServerRestart, true)]
        private static bool HandleServerRestartCommand(CommandHandler handler, StringArguments args)
        {
            return ShutdownServer(args, handler, ShutdownMask.Restart, ShutdownExitCode.Restart);
        }

        [Command("cancel", RBACPermissions.CommandServerRestartCancel, true)]
        private static bool HandleServerShutDownCancelCommand(CommandHandler handler)
        {
            var timer = handler.WorldManager.ShutdownCancel();

            if (timer != 0)
                handler.SendSysMessage(CypherStrings.ShutdownCancelled, timer);

            return true;
        }
    }

    [CommandGroup("set")]
    private class SetCommands
    {
        [Command("closed", RBACPermissions.CommandServerSetClosed, true)]
        private static bool HandleServerSetClosedCommand(CommandHandler handler, StringArguments args)
        {
            var arg1 = args.NextString();

            if (arg1.Equals("on", StringComparison.OrdinalIgnoreCase))
            {
                handler.SendSysMessage(CypherStrings.WorldClosed);
                handler.WorldManager.SetClosed(true);

                return true;
            }
            else if (arg1.Equals("off", StringComparison.OrdinalIgnoreCase))
            {
                handler.SendSysMessage(CypherStrings.WorldOpened);
                handler.WorldManager.SetClosed(false);

                return true;
            }

            handler.SendSysMessage(CypherStrings.UseBol);

            return false;
        }

        [Command("difftime", RBACPermissions.CommandServerSetDifftime, true)]
        private static bool HandleServerSetDiffTimeCommand(CommandHandler handler, StringArguments args)
        {
            if (args.Empty())
                return false;

            var newTimeStr = args.NextString();

            if (newTimeStr.IsEmpty())
                return false;

            if (!int.TryParse(newTimeStr, out var newTime) || newTime < 0)
                return false;

            //handler.WorldManager.SetRecordDiffInterval(newTime);
            //printf("Record diff every %i ms\n", newTime);

            return true;
        }

        [Command("loglevel", RBACPermissions.CommandServerSetLoglevel, true)]
        private static bool HandleServerSetLogLevelCommand(CommandHandler handler, string type, string name, int level)
        {
            if (name.IsEmpty() || level < 0 || (type != "a" && type != "l"))
                return false;

            return false;
        }

        [Command("motd", RBACPermissions.CommandServerSetMotd, true)]
        private static bool HandleServerSetMotdCommand(CommandHandler handler, StringArguments args)
        {
            handler.WorldManager.SetMotd(args.NextString(""));
            handler.SendSysMessage(CypherStrings.MotdNew, args.GetString());

            return true;
        }
    }

    [CommandGroup("shutdown")]
    private class ShutdownCommands
    {
        [Command("force", RBACPermissions.CommandServerShutdownCancel, true)]
        private static bool HandleServerForceShutDownCommand(CommandHandler handler, StringArguments args)
        {
            return ShutdownServer(args, handler, ShutdownMask.Force, ShutdownExitCode.Shutdown);
        }

        [Command("cancel", RBACPermissions.CommandServerShutdownCancel, true)]
        private static bool HandleServerShutDownCancelCommand(CommandHandler handler)
        {
            var timer = handler.WorldManager.ShutdownCancel();

            if (timer != 0)
                handler.SendSysMessage(CypherStrings.ShutdownCancelled, timer);

            return true;
        }

        [Command("", RBACPermissions.CommandServerShutdown, true)]
        private static bool HandleServerShutDownCommand(CommandHandler handler, StringArguments args)
        {
            return ShutdownServer(args, handler, 0, ShutdownExitCode.Shutdown);
        }
    }
}