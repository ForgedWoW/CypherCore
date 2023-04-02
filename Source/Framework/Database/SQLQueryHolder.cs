// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Framework.Database;

public class SQLQueryHolder<T>
{
    public Dictionary<T, PreparedStatement> m_queries = new();
    private readonly Dictionary<T, SQLResult> _results = new();

    public SQLResult GetResult(T index)
    {
        if (!_results.TryGetValue(index, out var result))
            return new SQLResult();

        return result;
    }

    public void SetQuery(T index, string sql, params object[] args)
    {
        SetQuery(index, new PreparedStatement(string.Format(sql, args)));
    }

    public void SetQuery(T index, PreparedStatement stmt)
    {
        m_queries[index] = stmt;
    }

    public void SetResult(T index, SQLResult result)
    {
        _results[index] = result;
    }
}