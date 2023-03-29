// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Database;

public class SQLQueryHolderTask<R> : ISqlOperation
{
    public SQLQueryHolder<R> QueryResults { get; private set; }

    public SQLQueryHolderTask(SQLQueryHolder<R> holder)
    {
        QueryResults = holder;
    }

    public bool Execute<T>(MySqlBase<T> mySqlBase)
    {
        if (QueryResults == null)
            return false;

        // execute all queries in the holder and pass the results
        foreach (var pair in QueryResults.m_queries)
            QueryResults.SetResult(pair.Key, mySqlBase.Query(pair.Value));

        return true;
    }
}