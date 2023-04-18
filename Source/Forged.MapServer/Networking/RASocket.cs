// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Forged.MapServer.Chat;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Cryptography;
using Framework.Database;
using Framework.Networking;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Networking;

public class RASocket : ISocket
{
    private readonly IConfiguration _configuration;
    private readonly LoginDatabase _loginDatabase;
    private readonly byte[] _receiveBuffer;
    private readonly IPAddress _remoteAddress;
    private readonly Socket _socket;
    private readonly WorldManager _worldManager;

    public RASocket(Socket socket, WorldManager worldManager, LoginDatabase loginDatabase, IConfiguration configuration)
    {
        _socket = socket;
        _worldManager = worldManager;
        _loginDatabase = loginDatabase;
        _configuration = configuration;
        _remoteAddress = (_socket.RemoteEndPoint as IPEndPoint)?.Address;
        _receiveBuffer = new byte[1024];
    }

    public bool IsOpen => _socket.Connected;

    public void Accept()
    {
        // wait 1 second for active connections to send negotiation request
        for (var counter = 0; counter < 10 && _socket.Available == 0; counter++)
            Thread.Sleep(100);

        if (_socket.Available > 0)
        {
            // Handle subnegotiation
            _socket.Receive(_receiveBuffer);

            // Send the end-of-negotiation packet
            byte[] reply =
            {
                0xFF, 0xF0
            };

            _socket.Send(reply);
        }

        Send("Authentication Required\r\n");
        Send("Email: ");
        var userName = ReadString();

        if (userName.IsEmpty())
        {
            CloseSocket();

            return;
        }

        Log.Logger.Information($"Accepting RA connection from user {userName} (IP: {_remoteAddress})");

        Send("Password: ");
        var password = ReadString();

        if (password.IsEmpty())
        {
            CloseSocket();

            return;
        }

        if (!CheckAccessLevelAndPassword(userName, password))
        {
            Send("Authentication failed\r\n");
            CloseSocket();

            return;
        }

        Log.Logger.Information($"User {userName} (IP: {_remoteAddress}) authenticated correctly to RA");

        // Authentication successful, send the motd
        foreach (var line in _worldManager.Motd)
            Send(line);

        Send("\r\n");

        // Read commands
        for (;;)
        {
            Send("\r\nForged>");
            var command = ReadString();

            if (!ProcessCommand(command))
                break;
        }

        CloseSocket();
    }

    public void CloseSocket()
    {
        if (_socket == null)
            return;

        try
        {
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }
        catch (Exception ex)
        {
            Log.Logger.Debug("WorldSocket.CloseSocket: {0} errored when shutting down socket: {1}", _remoteAddress.ToString(), ex.Message);
        }
    }

    public bool Update()
    {
        return IsOpen;
    }

    private bool CheckAccessLevelAndPassword(string email, string password)
    {
        //"SELECT a.id, a.username FROM account a LEFT JOIN battlenet_accounts ba ON a.battlenet_account = ba.id WHERE ba.email = ?"
        var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.SEL_BNET_GAME_ACCOUNT_LIST);
        stmt.AddValue(0, email);
        var result = _loginDatabase.Query(stmt);

        if (result.IsEmpty())
        {
            Log.Logger.Information($"User {email} does not exist in database");

            return false;
        }

        var accountId = result.Read<uint>(0);
        var username = result.Read<string>(1);

        stmt = _loginDatabase.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_ACCESS_BY_ID);
        stmt.AddValue(0, accountId);
        result = _loginDatabase.Query(stmt);

        if (result.IsEmpty())
        {
            Log.Logger.Information($"User {email} has no privilege to login");

            return false;
        }

        //"SELECT SecurityLevel, RealmID FROM account_access WHERE AccountID = ? and (RealmID = ? OR RealmID = -1) ORDER BY SecurityLevel desc");
        if (result.Read<byte>(0) < _configuration.GetDefaultValue("Ra:MinLevel", (byte)AccountTypes.Administrator))
        {
            Log.Logger.Information($"User {email} has no privilege to login");

            return false;
        }

        if (result.Read<int>(1) != -1)
        {
            Log.Logger.Information($"User {email} has to be assigned on all realms (with RealmID = '-1')");

            return false;
        }

        stmt = _loginDatabase.GetPreparedStatement(LoginStatements.SEL_CHECK_PASSWORD);
        stmt.AddValue(0, accountId);
        result = _loginDatabase.Query(stmt);

        if (!result.IsEmpty())
        {
            var salt = result.Read<byte[]>(0);
            var verifier = result.Read<byte[]>(1);

            if (SRP6.CheckLogin(username, password, salt, verifier))
                return true;
        }

        Log.Logger.Information($"Wrong password for user: {email}");

        return false;
    }

    private void CommandPrint(string text)
    {
        if (text.IsEmpty())
            return;

        Send(text);
    }

    private bool ProcessCommand(string command)
    {
        if (command.Length == 0)
            return false;

        Log.Logger.Information($"Received command: {command}");

        // handle quit, exit and logout commands to terminate connection
        if (command is "quit" or "exit" or "logout")
        {
            Send("Closing\r\n");

            return false;
        }

        RemoteAccessHandler cmd = new(CommandPrint, _worldManager);
        cmd.ParseCommands(command);

        return true;
    }

    private string ReadString()
    {
        try
        {
            var str = "";

            do
            {
                var bytes = _socket.Receive(_receiveBuffer);

                if (bytes == 0)
                    return "";

                str = string.Concat(str, Encoding.UTF8.GetString(_receiveBuffer, 0, bytes));
            } while (!str.Contains("\n"));

            return str.TrimEnd('\r', '\n');
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex);

            return "";
        }
    }

    private void Send(string str)
    {
        if (!IsOpen)
            return;

        _socket.Send(Encoding.UTF8.GetBytes(str));
    }
}