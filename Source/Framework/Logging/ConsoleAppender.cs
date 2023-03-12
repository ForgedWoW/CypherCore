// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

class ConsoleAppender : Appender
{
	readonly ConsoleColor[] _consoleColor;

	public ConsoleAppender(byte id, string name, LogLevel level, AppenderFlags flags) : base(id, name, level, flags)
	{
		_consoleColor = new[]
		{
			ConsoleColor.White, ConsoleColor.White, ConsoleColor.Gray, ConsoleColor.Green, ConsoleColor.Yellow, ConsoleColor.Red, ConsoleColor.Blue
		};
	}

	public override void _write(LogMessage message)
	{
		Console.ForegroundColor = _consoleColor[(int)message.level];
		Console.WriteLine(message.prefix + message.text);
		Console.ResetColor();
	}

	public override AppenderType GetAppenderType()
	{
		return AppenderType.Console;
	}
}