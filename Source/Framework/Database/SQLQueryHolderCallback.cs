// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Database;

public class SQLQueryHolderCallback<R> : ISqlCallback
{
    private readonly SQLQueryHolderTask<R> _future;
    private Action<SQLQueryHolder<R>> _callback;

    public SQLQueryHolderCallback(SQLQueryHolderTask<R> future)
    {
        _future = future;
    }

    public bool InvokeIfReady()
    {
        return _callback == null;
    }

    public void AfterComplete(Action<SQLQueryHolder<R>> callback)
    {
        _callback = callback;
    }

    public virtual void QueryExecuted(bool success)
    {
        if (success && _future.QueryResults != null)
            _callback(_future.QueryResults);

        _callback = null;
    }
}