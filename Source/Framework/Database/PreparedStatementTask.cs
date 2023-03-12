// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Database;

public class PreparedStatementTask : ISqlOperation
{
	readonly PreparedStatement m_stmt;
	readonly bool _needsResult;
	public SQLResult Result { get; private set; }

	public PreparedStatementTask(PreparedStatement stmt, bool needsResult = false)
	{
		m_stmt = stmt;
		_needsResult = needsResult;
	}

	public bool Execute<T>(MySqlBase<T> mySqlBase)
	{
		if (_needsResult)
		{
			var result = mySqlBase.Query(m_stmt);

			if (result == null)
			{
				Result = new SQLResult();

				return false;
			}

			Result = result;

			return true;
		}

		return mySqlBase.DirectExecute(m_stmt);
	}
}