// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using MySqlConnector;

namespace Framework.Database;

public class MySqlConnectionInfo
{
    public string Host;
    public string PortOrSocket;
    public bool UseSSL;
    public string Username;
    public string Password;
    public string Database;
    public int Poolsize;

    public MySqlConnection GetConnection()
    {
        return new MySqlConnection($"Server={Host};Port={PortOrSocket};User Id={Username};Password={Password};Database={Database};Allow User Variables=True;Pooling=true;ConnectionIdleTimeout=1800;Command Timeout=0");
    }
}