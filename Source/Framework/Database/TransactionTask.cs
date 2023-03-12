// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using MySqlConnector;

namespace Framework.Database;

public class TransactionTask : ISqlOperation
{
	public static object _deadlockLock = new();

	readonly SQLTransaction m_trans;

	public TransactionTask(SQLTransaction trans)
	{
		m_trans = trans;
	}

	public virtual bool Execute<T>(MySqlBase<T> mySqlBase)
	{
		var errorCode = TryExecute(mySqlBase);

		if (errorCode == MySqlErrorCode.None)
			return true;

		if (errorCode == MySqlErrorCode.LockDeadlock)
			// Make sure only 1 async thread retries a transaction so they don't keep dead-locking each other
			lock (_deadlockLock)
			{
				byte loopBreaker = 5; // Handle MySQL Errno 1213 without extending deadlock to the core itself

				for (byte i = 0; i < loopBreaker; ++i)
					if (TryExecute(mySqlBase) == MySqlErrorCode.None)
						return true;
			}

		return false;
	}

	public MySqlErrorCode TryExecute<T>(MySqlBase<T> mySqlBase)
	{
		return mySqlBase.DirectCommitTransaction(m_trans);
	}
}