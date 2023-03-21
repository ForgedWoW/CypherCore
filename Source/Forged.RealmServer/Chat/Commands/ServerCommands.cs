// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Configuration;
using Framework.Constants;
using Framework.IO;

namespace Forged.RealmServer.Chat;

[CommandGroup("server")]
class ServerCommands
{
	[Command("corpses", RBACPermissions.CommandServerCorpses, true)]
	static bool HandleServerCorpsesCommand(CommandHandler handler)
	{
		Global.WorldMgr.RemoveOldCorpses();

		return true;
	}

	[Command("debug", RBACPermissions.CommandServerCorpses, true)]
	static bool HandleServerDebugCommand(CommandHandler handler)
	{
		return false; //todo fix me
	}

	[Command("exit", RBACPermissions.CommandServerExit, true)]
	static bool HandleServerExitCommand(CommandHandler handler)
	{
		handler.SendSysMessage(CypherStrings.CommandExit);
		Global.WorldMgr.StopNow(ShutdownExitCode.Shutdown);

		return true;
	}

	[Command("info", RBACPermissions.CommandServerInfo, true)]
	static bool HandleServerInfoCommand(CommandHandler handler)
	{
		var playersNum = Global.WorldMgr.PlayerCount;
		var maxPlayersNum = Global.WorldMgr.MaxPlayerCount;
		var activeClientsNum = Global.WorldMgr.ActiveSessionCount;
		var queuedClientsNum = Global.WorldMgr.QueuedSessionCount;
		var maxActiveClientsNum = Global.WorldMgr.MaxActiveSessionCount;
		var maxQueuedClientsNum = Global.WorldMgr.MaxQueuedSessionCount;
		var uptime = Time.secsToTimeString(GameTime.GetUptime());
		var updateTime = Global.WorldMgr.WorldUpdateTime.GetLastUpdateTime();

		handler.SendSysMessage(CypherStrings.ConnectedPlayers, playersNum, maxPlayersNum);
		handler.SendSysMessage(CypherStrings.ConnectedUsers, activeClientsNum, maxActiveClientsNum, queuedClientsNum, maxQueuedClientsNum);
		handler.SendSysMessage(CypherStrings.Uptime, uptime);
		handler.SendSysMessage(CypherStrings.UpdateDiff, updateTime);

		// Can't use Global.WorldMgr.ShutdownMsg here in case of console command
		if (Global.WorldMgr.IsShuttingDown)
			handler.SendSysMessage(CypherStrings.ShutdownTimeleft, Time.secsToTimeString(Global.WorldMgr.ShutDownTimeLeft));

		return true;
	}

	[Command("motd", RBACPermissions.CommandServerMotd, true)]
	static bool HandleServerMotdCommand(CommandHandler handler)
	{
		var motd = "";

		foreach (var line in Global.WorldMgr.Motd)
			motd += line;

		handler.SendSysMessage(CypherStrings.MotdCurrent, motd);

		return true;
	}

	[Command("plimit", RBACPermissions.CommandServerPlimit, true)]
	static bool HandleServerPLimitCommand(CommandHandler handler, StringArguments args)
	{
		if (!args.Empty())
		{
			var paramStr = args.NextString();

			if (string.IsNullOrEmpty(paramStr))
				return false;

			switch (paramStr.ToLower())
			{
				case "player":
					Global.WorldMgr.PlayerSecurityLimit = AccountTypes.Player;

					break;
				case "moderator":
					Global.WorldMgr.PlayerSecurityLimit = AccountTypes.Moderator;

					break;
				case "gamemaster":
					Global.WorldMgr.PlayerSecurityLimit = AccountTypes.GameMaster;

					break;
				case "administrator":
					Global.WorldMgr.PlayerSecurityLimit = AccountTypes.Administrator;

					break;
				case "reset":
					Global.WorldMgr.PlayerAmountLimit = ConfigMgr.GetDefaultValue<uint>("PlayerLimit", 100);
					Global.WorldMgr.LoadDBAllowedSecurityLevel();

					break;
				default:
					if (!int.TryParse(paramStr, out var value))
						return false;

					if (value < 0)
						Global.WorldMgr.PlayerSecurityLimit = (AccountTypes)(-value);
					else
						Global.WorldMgr.PlayerAmountLimit = (uint)value;

					break;
			}
		}

		var playerAmountLimit = Global.WorldMgr.PlayerAmountLimit;
		var allowedAccountType = Global.WorldMgr.PlayerSecurityLimit;
		string secName;

		switch (allowedAccountType)
		{
			case AccountTypes.Player:
				secName = "Player";

				break;
			case AccountTypes.Moderator:
				secName = "Moderator";

				break;
			case AccountTypes.GameMaster:
				secName = "Gamemaster";

				break;
			case AccountTypes.Administrator:
				secName = "Administrator";

				break;
			default:
				secName = "<unknown>";

				break;
		}

		handler.SendSysMessage("Player limits: amount {0}, min. security level {1}.", playerAmountLimit, secName);

		return true;
	}

	static bool IsOnlyUser(WorldSession mySession)
	{
		// check if there is any session connected from a different address
		var myAddr = mySession ? mySession.RemoteAddress : "";
		var sessions = Global.WorldMgr.AllSessions;

		foreach (var session in sessions)
			if (session && myAddr != session.RemoteAddress)
				return false;

		return true;
	}

	static bool ParseExitCode(string exitCodeStr, out int exitCode)
	{
		if (!int.TryParse(exitCodeStr, out exitCode))
			return false;

		// Handle atoi() errors
		if (exitCode == 0 && (exitCodeStr[0] != '0' || (exitCodeStr.Length > 1 && exitCodeStr[1] != '\0')))
			return false;

		// Exit code should be in range of 0-125, 126-255 is used
		// in many shells for their own return codes and code > 255
		// is not supported in many others
		if (exitCode < 0 || exitCode > 125)
			return false;

		return true;
	}

	static bool ShutdownServer(StringArguments args, CommandHandler handler, ShutdownMask shutdownMask, ShutdownExitCode defaultExitCode)
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
		if (delay < WorldConfig.GetIntValue(WorldCfg.ForceShutdownThreshold) && !shutdownMask.HasAnyFlag(ShutdownMask.Force) && !IsOnlyUser(handler.Session))
		{
			delay = WorldConfig.GetIntValue(WorldCfg.ForceShutdownThreshold);
			handler.SendSysMessage(CypherStrings.ShutdownDelayed, delay);
		}

		Global.WorldMgr.ShutdownServ((uint)delay, shutdownMask, (ShutdownExitCode)exitCode, reason);

		return true;
	}

	[CommandGroup("idleRestart")]
	class IdleRestartCommands
	{
		[Command("", RBACPermissions.CommandServerIdlerestart, true)]
		static bool HandleServerIdleRestartCommand(CommandHandler handler, StringArguments args)
		{
			return ShutdownServer(args, handler, ShutdownMask.Restart | ShutdownMask.Idle, ShutdownExitCode.Restart);
		}

		[Command("cancel", RBACPermissions.CommandServerIdlerestartCancel, true)]
		static bool HandleServerShutDownCancelCommand(CommandHandler handler)
		{
			var timer = Global.WorldMgr.ShutdownCancel();

			if (timer != 0)
				handler.SendSysMessage(CypherStrings.ShutdownCancelled, timer);

			return true;
		}
	}

	[CommandGroup("idleshutdown")]
	class IdleshutdownCommands
	{
		[Command("", RBACPermissions.CommandServerIdleshutdown, true)]
		static bool HandleServerIdleShutDownCommand(CommandHandler handler, StringArguments args)
		{
			return ShutdownServer(args, handler, ShutdownMask.Idle, ShutdownExitCode.Shutdown);
		}

		[Command("cancel", RBACPermissions.CommandServerIdleshutdownCancel, true)]
		static bool HandleServerShutDownCancelCommand(CommandHandler handler)
		{
			var timer = Global.WorldMgr.ShutdownCancel();

			if (timer != 0)
				handler.SendSysMessage(CypherStrings.ShutdownCancelled, timer);

			return true;
		}
	}

	[CommandGroup("restart")]
	class RestartCommands
	{
		[Command("", RBACPermissions.CommandServerRestart, true)]
		static bool HandleServerRestartCommand(CommandHandler handler, StringArguments args)
		{
			return ShutdownServer(args, handler, ShutdownMask.Restart, ShutdownExitCode.Restart);
		}

		[Command("cancel", RBACPermissions.CommandServerRestartCancel, true)]
		static bool HandleServerShutDownCancelCommand(CommandHandler handler)
		{
			var timer = Global.WorldMgr.ShutdownCancel();

			if (timer != 0)
				handler.SendSysMessage(CypherStrings.ShutdownCancelled, timer);

			return true;
		}

		[Command("force", RBACPermissions.CommandServerRestartCancel, true)]
		static bool HandleServerForceRestartCommand(CommandHandler handler, StringArguments args)
		{
			return ShutdownServer(args, handler, ShutdownMask.Force | ShutdownMask.Restart, ShutdownExitCode.Restart);
		}
	}

	[CommandGroup("shutdown")]
	class ShutdownCommands
	{
		[Command("", RBACPermissions.CommandServerShutdown, true)]
		static bool HandleServerShutDownCommand(CommandHandler handler, StringArguments args)
		{
			return ShutdownServer(args, handler, 0, ShutdownExitCode.Shutdown);
		}

		[Command("cancel", RBACPermissions.CommandServerShutdownCancel, true)]
		static bool HandleServerShutDownCancelCommand(CommandHandler handler)
		{
			var timer = Global.WorldMgr.ShutdownCancel();

			if (timer != 0)
				handler.SendSysMessage(CypherStrings.ShutdownCancelled, timer);

			return true;
		}

		[Command("force", RBACPermissions.CommandServerShutdownCancel, true)]
		static bool HandleServerForceShutDownCommand(CommandHandler handler, StringArguments args)
		{
			return ShutdownServer(args, handler, ShutdownMask.Force, ShutdownExitCode.Shutdown);
		}
	}

	[CommandGroup("set")]
	class SetCommands
	{
		[Command("difftime", RBACPermissions.CommandServerSetDifftime, true)]
		static bool HandleServerSetDiffTimeCommand(CommandHandler handler, StringArguments args)
		{
			if (args.Empty())
				return false;

			var newTimeStr = args.NextString();

			if (newTimeStr.IsEmpty())
				return false;

			if (!int.TryParse(newTimeStr, out var newTime) || newTime < 0)
				return false;

			//Global.WorldMgr.SetRecordDiffInterval(newTime);
			//printf("Record diff every %i ms\n", newTime);

			return true;
		}

		[Command("loglevel", RBACPermissions.CommandServerSetLoglevel, true)]
		static bool HandleServerSetLogLevelCommand(CommandHandler handler, string type, string name, int level)
		{
			if (name.IsEmpty() || level < 0 || (type != "a" && type != "l"))
				return false;

			return Log.SetLogLevel(name, level, type == "l");
		}

		[Command("motd", RBACPermissions.CommandServerSetMotd, true)]
		static bool HandleServerSetMotdCommand(CommandHandler handler, StringArguments args)
		{
			Global.WorldMgr.SetMotd(args.NextString(""));
			handler.SendSysMessage(CypherStrings.MotdNew, args.GetString());

			return true;
		}

		[Command("closed", RBACPermissions.CommandServerSetClosed, true)]
		static bool HandleServerSetClosedCommand(CommandHandler handler, StringArguments args)
		{
			var arg1 = args.NextString();

			if (arg1.Equals("on", StringComparison.OrdinalIgnoreCase))
			{
				handler.SendSysMessage(CypherStrings.WorldClosed);
				Global.WorldMgr.SetClosed(true);

				return true;
			}
			else if (arg1.Equals("off", StringComparison.OrdinalIgnoreCase))
			{
				handler.SendSysMessage(CypherStrings.WorldOpened);
				Global.WorldMgr.SetClosed(false);

				return true;
			}

			handler.SendSysMessage(CypherStrings.UseBol);

			return false;
		}
	}
}