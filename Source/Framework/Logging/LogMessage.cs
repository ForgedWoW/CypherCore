// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

class LogMessage
{
	public LogLevel level;
	public LogFilter type;
	public string text;
	public string prefix;
	public string dynamicName;
	public DateTime mtime;

	public LogMessage(LogLevel _level, LogFilter _type, string _text)
	{
		level = _level;
		type = _type;
		text = _text;
		mtime = DateTime.Now;
	}
}