// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Framework.Collections;
using Framework.Configuration;

public class Log
{
	static readonly Dictionary<byte, Appender> appenders = new();
	static readonly Dictionary<LogFilter, Logger> loggers = new();
	static readonly string m_logsDir;
	static byte AppenderId;

	static LogLevel lowestLogLevel;

	static Log()
	{
		m_logsDir = AppContext.BaseDirectory + ConfigMgr.GetDefaultValue("LogsDir", "");
		lowestLogLevel = LogLevel.Fatal;

		foreach (var appenderName in ConfigMgr.GetKeysByString("Appender."))
			CreateAppenderFromConfig(appenderName);

		foreach (var loggerName in ConfigMgr.GetKeysByString("Logger."))
			CreateLoggerFromConfig(loggerName);

		// Bad config configuration, creating default config
		if (!loggers.ContainsKey(LogFilter.Server))
		{
			Console.WriteLine("Wrong Loggers configuration. Review your Logger config section.\nCreating default loggers [Server (Info)] to console\n");

			loggers.Clear();
			appenders.Clear();

			var id = NextAppenderId();

			var appender = new ConsoleAppender(id, "Console", LogLevel.Debug, AppenderFlags.None);
			appenders[id] = appender;

			var serverLogger = new Logger("Server", LogLevel.Error);
			serverLogger.addAppender(id, appender);
			loggers[LogFilter.Server] = serverLogger;
		}

		outInfo(LogFilter.Server, @"```````````````````````````````````````````````````````````````````````````````````````");
		outInfo(LogFilter.Server, @"███████╗ ██████╗ ██████╗  ██████╗ ███████╗██████╗      ██████╗ ██████╗ ██████╗ ███████╗");
		outInfo(LogFilter.Server, @"██╔════╝██╔═══██╗██╔══██╗██╔════╝ ██╔════╝██╔══██╗    ██╔════╝██╔═══██╗██╔══██╗██╔════╝");
		outInfo(LogFilter.Server, @"█████╗  ██║   ██║██████╔╝██║  ███╗█████╗  ██║  ██║    ██║     ██║   ██║██████╔╝█████╗  ");
		outInfo(LogFilter.Server, @"██╔══╝  ██║   ██║██╔══██╗██║   ██║██╔══╝  ██║  ██║    ██║     ██║   ██║██╔══██╗██╔══╝  ");
		outInfo(LogFilter.Server, @"██║     ╚██████╔╝██║  ██║╚██████╔╝███████╗██████╔╝    ╚██████╗╚██████╔╝██║  ██║███████╗");
		outInfo(LogFilter.Server, @"╚═╝      ╚═════╝ ╚═╝  ╚═╝ ╚═════╝ ╚══════╝╚═════╝      ╚═════╝ ╚═════╝ ╚═╝  ╚═╝╚══════╝");
		outInfo(LogFilter.Server, @"                                                                     A Cypher Core Fork");
		outInfo(LogFilter.Server, @"```````````````````````````````````````````````````````````````````````````````````````");
		outInfo(LogFilter.Server, "https://github.com/ForgedWoW/ForgedCore \r\n");
	}

	public static void outLog(LogFilter type, LogLevel level, string text, params object[] args)
	{
		if (!ShouldLog(type, level))
			return;

		outMessage(type, level, text, args);
	}

	public static void outInfo(LogFilter type, string text, params object[] args)
	{
		if (!ShouldLog(type, LogLevel.Info))
			return;

		outMessage(type, LogLevel.Info, text, args);
	}

	public static void outWarn(LogFilter type, string text, params object[] args)
	{
		if (!ShouldLog(type, LogLevel.Warn))
			return;

		outMessage(type, LogLevel.Warn, text, args);
	}

	[Conditional("DEBUG")]
	public static void outDebug(LogFilter type, string text, params object[] args)
	{
		if (!ShouldLog(type, LogLevel.Debug))
			return;

		outMessage(type, LogLevel.Debug, text, args);
	}

	public static void outError(LogFilter type, string text, params object[] args)
	{
		if (!ShouldLog(type, LogLevel.Error))
			return;

		outMessage(type, LogLevel.Error, text, args);
	}

	public static void outException(Exception ex, [CallerMemberName] string memberName = "")
	{
		if (!ShouldLog(LogFilter.Server, LogLevel.Fatal))
			return;

		outMessage(LogFilter.Server, LogLevel.Fatal, "CallingMember: {0} ExceptionMessage: {1}\n{2}", memberName, ex.Message, ex.StackTrace);

		if (ex.InnerException != null)
			outException(ex.InnerException, memberName);
	}

	public static void outFatal(LogFilter type, string text, params object[] args)
	{
		if (!ShouldLog(type, LogLevel.Fatal))
			return;

		outMessage(type, LogLevel.Fatal, text, args);
	}

	public static void outTrace(LogFilter type, string text, params object[] args)
	{
		if (!ShouldLog(type, LogLevel.Trace))
			return;

		outMessage(type, LogLevel.Trace, text, args);
	}

	public static void outCommand(uint accountId, string text, params object[] args)
	{
		if (!ShouldLog(LogFilter.Commands, LogLevel.Info))
			return;

		var msg = new LogMessage(LogLevel.Info, LogFilter.Commands, string.Format(text, args));
		msg.dynamicName = accountId.ToString();

		var logger = GetLoggerByType(LogFilter.Commands);
		logger.write(msg);
	}

	public static bool SetLogLevel(string name, int newLeveli, bool isLogger = true)
	{
		if (newLeveli < 0)
			return false;

		var newLevel = (LogLevel)newLeveli;

		if (isLogger)
		{
			foreach (var logger in loggers.Values)
				if (logger.getName() == name)
				{
					logger.setLogLevel(newLevel);

					if (newLevel != LogLevel.Disabled && newLevel < lowestLogLevel)
						lowestLogLevel = newLevel;

					return true;
				}

			return false;
		}
		else
		{
			var appender = GetAppenderByName(name);

			if (appender == null)
				return false;

			appender.setLogLevel(newLevel);
		}

		return true;
	}

	public static void SetRealmId(uint id)
	{
		foreach (var appender in appenders.Values)
			appender.setRealmId(id);
	}

	static bool ShouldLog(LogFilter type, LogLevel level)
	{
		// Don't even look for a logger if the LogLevel is lower than lowest log levels across all loggers
		if (level < lowestLogLevel)
			return false;

		var logger = GetLoggerByType(type);

		if (logger == null)
			return false;

		var logLevel = logger.getLogLevel();

		return logLevel != LogLevel.Disabled && logLevel <= level;
	}

	static void outMessage(LogFilter type, LogLevel level, string text, params object[] args)
	{
		var logger = GetLoggerByType(type);
		logger.write(new LogMessage(level, type, string.Format(text, args)));
	}

	static byte NextAppenderId()
	{
		return AppenderId++;
	}

	static void CreateAppenderFromConfig(string appenderName)
	{
		if (string.IsNullOrEmpty(appenderName))
			return;

		var options = ConfigMgr.GetDefaultValue(appenderName, "");
		var tokens = new StringArray(options, ',');
		var name = appenderName.Substring(9);

		if (tokens.Length < 2)
		{
			Console.WriteLine("Log.CreateAppenderFromConfig: Wrong configuration for appender {0}. Config line: {1}", name, options);

			return;
		}

		var flags = AppenderFlags.None;
		var type = (AppenderType)uint.Parse(tokens[0]);
		var level = (LogLevel)uint.Parse(tokens[1]);

		if (level > LogLevel.Fatal)
		{
			Console.WriteLine("Log.CreateAppenderFromConfig: Wrong Log Level {0} for appender {1}\n", level, name);

			return;
		}

		if (tokens.Length > 2)
			flags = (AppenderFlags)uint.Parse(tokens[2]);

		var id = NextAppenderId();

		switch (type)
		{
			case AppenderType.Console:
			{
				var appender = new ConsoleAppender(id, name, level, flags);
				appenders[id] = appender;

				break;
			}
			case AppenderType.File:
			{
				string filename;

				if (tokens.Length < 4)
				{
					if (name != "Server")
					{
						Console.WriteLine("Log.CreateAppenderFromConfig: Missing file name for appender {0}", name);

						return;
					}

					filename = Process.GetCurrentProcess().ProcessName + ".log";
				}
				else
				{
					filename = tokens[3];
				}

				appenders[id] = new FileAppender(id, name, level, filename, m_logsDir, flags);

				break;
			}
			case AppenderType.DB:
			{
				appenders[id] = new DBAppender(id, name, level);

				break;
			}
			default:
				Console.WriteLine("Log.CreateAppenderFromConfig: Unknown type {0} for appender {1}", type, name);

				break;
		}
	}

	static void CreateLoggerFromConfig(string appenderName)
	{
		if (string.IsNullOrEmpty(appenderName))
			return;

		var name = appenderName.Substring(7);

		var options = ConfigMgr.GetDefaultValue(appenderName, "");

		if (string.IsNullOrEmpty(options))
		{
			Console.WriteLine("Log.CreateLoggerFromConfig: Missing config option Logger.{0}", name);

			return;
		}

		var tokens = new StringArray(options, ',');

		var type = name.ToEnum<LogFilter>();

		if (loggers.ContainsKey(type))
		{
			Console.WriteLine("Error while configuring Logger {0}. Already defined", name);

			return;
		}

		var level = (LogLevel)uint.Parse(tokens[0]);

		if (level > LogLevel.Fatal)
		{
			Console.WriteLine("Log.CreateLoggerFromConfig: Wrong Log Level {0} for logger {1}", type, name);

			return;
		}

		if (level < lowestLogLevel)
			lowestLogLevel = level;

		Logger logger = new(name, level);

		var i = 0;
		var ss = new StringArray(tokens[1], ' ');

		while (i < ss.Length)
		{
			var str = ss[i++];
			var appender = GetAppenderByName(str);

			if (appender == null)
				Console.WriteLine("Error while configuring Appender {0} in Logger {1}. Appender does not exist", str, name);
			else
				logger.addAppender(appender.getId(), appender);
		}

		loggers[type] = logger;
	}

	static Appender GetAppenderByName(string name)
	{
		return appenders.First(p => p.Value.getName() == name).Value;
	}

	static Logger GetLoggerByType(LogFilter type)
	{
		if (loggers.TryGetValue(type, out var logger))
			return logger;

		var typeString = type.ToString();

		var index = 1;

		for (; index < typeString.Length - 1; ++index)
			if (char.IsUpper(typeString[index]))
				break;

		typeString = typeString.Substring(0, index);

		if (typeString.IsEmpty())
			return null;

		if (!Enum.TryParse(typeString, out LogFilter parentLogger))
			return null;

		return GetLoggerByType(parentLogger);
	}
}