// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using MySqlConnector;

namespace Framework.Database;

public class SQLTransaction
{
    public List<MySqlCommand> commands { get; }

    public SQLTransaction()
    {
        commands = new List<MySqlCommand>();
    }

    public void Append(PreparedStatement stmt)
    {
        MySqlCommand cmd = new(stmt.CommandText);

        foreach (var parameter in stmt.Parameters)
            cmd.Parameters.AddWithValue("@" + parameter.Key, parameter.Value);

        commands.Add(cmd);
    }

    public void Append(string sql, params object[] args)
    {
        commands.Add(new MySqlCommand(string.Format(sql, args)));
    }
}