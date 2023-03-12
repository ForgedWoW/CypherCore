// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Database;

public static class SQLEx
{
	public static bool IsEmpty(this SQLResult result)
	{
		if (result == null)
			return true;

		if (result.Reader == null)
			return true;

		return result.Reader.IsClosed || !result.Reader.HasRows || result.Reader.FieldCount == 0;
	}
}