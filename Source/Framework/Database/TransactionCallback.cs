// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Database;

public class TransactionCallback : ISqlCallback
{
    private readonly TransactionWithResultTask m_future;
    private Action<bool> _callback;

	public TransactionCallback(TransactionWithResultTask future)
	{
		m_future = future;
	}

	public bool InvokeIfReady()
	{
		return _callback == null;
	}

	public void AfterComplete(Action<bool> callback)
	{
		_callback = callback;
	}

	public virtual void QueryExecuted(bool success)
	{
		if (success)
			if (m_future.Result.HasValue)
				_callback(m_future.Result.Value);

		_callback = null;
	}
}