// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using MySqlConnector;

namespace Framework.Database;

public class MySqlConnectionInfo
{
    public string Database;
    public string Host;
    public string Password;
    public int Poolsize;
    public string PortOrSocket;
    public string Username;
    public bool UseSSL;

    public MySqlConnection GetConnection()
    {
        return new MySqlConnection($"Server={Host};Port={PortOrSocket};User Id={Username};Password={Password};Database={Database};Allow User Variables=True;Pooling=true;ConnectionIdleTimeout=1800;Command Timeout=0");
    }
}