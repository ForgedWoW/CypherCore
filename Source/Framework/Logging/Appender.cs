// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Text;

abstract class Appender
{
	readonly byte _id;
	readonly string _name;
	readonly AppenderFlags _flags;
	LogLevel _level;

	protected Appender(byte id, string name, LogLevel level = LogLevel.Disabled, AppenderFlags flags = AppenderFlags.None)
	{
		_id = id;
		_name = name;
		_level = level;
		_flags = flags;
	}

	public void Write(LogMessage message)
	{
		if (_level == LogLevel.Disabled || (_level != LogLevel.Fatal && _level > message.level))
			return;

		StringBuilder ss = new();

		if (_flags.HasAnyFlag(AppenderFlags.PrefixTimestamp))
			ss.AppendFormat("[{0:MM/dd/yyyy HH:mm:ss}] ", message.mtime);

        if (_flags.HasAnyFlag(AppenderFlags.PrefixLogFilterType))
            ss.AppendFormat("({0}) ", message.type);

        if (_flags.HasAnyFlag(AppenderFlags.PrefixLogLevel))
			ss.AppendFormat("<{0}> ", message.level);

		message.prefix = ss.ToString();
		_write(message);
	}

	public abstract void _write(LogMessage message);

	public byte getId()
	{
		return _id;
	}

	public string getName()
	{
		return _name;
	}

	public abstract AppenderType GetAppenderType();

	public virtual void setRealmId(uint realmId) { }

	public void setLogLevel(LogLevel level)
	{
		_level = level;
	}
}