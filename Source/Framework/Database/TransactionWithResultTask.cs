// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using MySqlConnector;

namespace Framework.Database;

public class TransactionWithResultTask : TransactionTask
{
	public bool? Result { get; private set; } = null;
	public TransactionWithResultTask(SQLTransaction trans) : base(trans) { }

	public override bool Execute<T>(MySqlBase<T> mySqlBase)
	{
		var errorCode = TryExecute(mySqlBase);

		if (errorCode == MySqlErrorCode.None)
		{
			Result = true;

			return true;
		}

		if (errorCode == MySqlErrorCode.LockDeadlock)
			// Make sure only 1 async thread retries a transaction so they don't keep dead-locking each other
			lock (_deadlockLock)
			{
				byte loopBreaker = 5; // Handle MySQL Errno 1213 without extending deadlock to the core itself

				for (byte i = 0; i < loopBreaker; ++i)
					if (TryExecute(mySqlBase) == MySqlErrorCode.None)
					{
						Result = true;

						return true;
					}
			}

		Result = false;

		return false;
	}
}