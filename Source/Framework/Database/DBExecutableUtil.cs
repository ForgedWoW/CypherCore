// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.IO;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Framework.Database;

public static class DBExecutableUtil
{
	static string _mysqlExecutablePath;

	public static string GetMySQLExecutable()
	{
		return _mysqlExecutablePath;
	}

	public static bool CheckExecutable(IConfiguration configuration)
	{
		var mysqlExePath = configuration.GetDefaultValue("MySQLExecutable", "");

		if (mysqlExePath.IsEmpty() || !File.Exists(mysqlExePath))
		{
			Log.Logger.Fatal($"Didn't find any executable MySQL binary at \'{mysqlExePath}\' or in path, correct the path in the *.conf (\"MySQLExecutable\").");

			return false;
		}

		// Correct the path to the cli
		_mysqlExecutablePath = mysqlExePath;

		return true;
	}
}