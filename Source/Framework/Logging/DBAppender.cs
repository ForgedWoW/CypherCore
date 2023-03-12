// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Database;

class DBAppender : Appender
{
	uint realmId;
	bool enabled;
	public DBAppender(byte id, string name, LogLevel level) : base(id, name, level) { }

	public override void _write(LogMessage message)
	{
		// Avoid infinite loop, PExecute triggers Logging with "sql.sql" type
		if (!enabled || message.type == LogFilter.Sql)
			return;

		var stmt = LoginDatabase.GetPreparedStatement(LoginStatements.INS_LOG);
		stmt.AddValue(0, Time.DateTimeToUnixTime(message.mtime));
		stmt.AddValue(1, realmId);
		stmt.AddValue(2, message.type.ToString());
		stmt.AddValue(3, (byte)message.level);
		stmt.AddValue(4, message.text);
		DB.Login.Execute(stmt);
	}

	public override AppenderType GetAppenderType()
	{
		return AppenderType.DB;
	}

	public override void setRealmId(uint _realmId)
	{
		enabled = true;
		realmId = _realmId;
	}
}