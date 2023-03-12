// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.IO;
using Framework.Configuration;

namespace Framework.Database;

public static class DBExecutableUtil
{
	static string mysqlExecutablePath;

	public static string GetMySQLExecutable()
	{
		return mysqlExecutablePath;
	}

	public static bool CheckExecutable()
	{
		var mysqlExePath = ConfigMgr.GetDefaultValue("MySQLExecutable", "");

		if (mysqlExePath.IsEmpty() || !File.Exists(mysqlExePath))
		{
			Log.outFatal(LogFilter.SqlUpdates, $"Didn't find any executable MySQL binary at \'{mysqlExePath}\' or in path, correct the path in the *.conf (\"MySQLExecutable\").");

			return false;
		}

		// Correct the path to the cli
		mysqlExecutablePath = mysqlExePath;

		return true;
	}
}