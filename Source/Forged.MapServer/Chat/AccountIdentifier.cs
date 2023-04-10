// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Accounts;
using Forged.MapServer.Server;
using Forged.MapServer.World;
using Framework.Constants;

namespace Forged.MapServer.Chat;

internal class AccountIdentifier
{
    private readonly AccountManager _accountManager;
    private readonly WorldManager _worldManager;
    private uint _id;
    private string _name;
    private WorldSession _session;
    public AccountIdentifier() { }

    public AccountIdentifier(WorldSession session, AccountManager accountManager, WorldManager worldManager)
    {
        _id = session.AccountId;
        _name = session.AccountName;
        _session = session;
        _accountManager = accountManager;
        _worldManager = worldManager;
    }

    public static AccountIdentifier FromTarget(CommandHandler handler)
    {
        var player = handler.Player;

        var target = player?.SelectedPlayer;

        var session = target?.Session;

        return session != null ? new AccountIdentifier(session, session.Player.AccountManager, session.Player.WorldMgr) : null;
    }

    public WorldSession GetConnectedSession()
    {
        return _session;
    }

    public uint GetID()
    {
        return _id;
    }

    public string GetName()
    {
        return _name;
    }

    public bool IsConnected()
    {
        return _session != null;
    }

    public ChatCommandResult TryConsume(CommandHandler handler, string args)
    {
        var next = CommandArgs.TryConsume(out var text, typeof(string), handler, args);

        if (!next.IsSuccessful)
            return next;

        // first try parsing as account name
        _name = text;
        _id = _accountManager.GetId(_name);
        _session = _worldManager.FindSession(_id);

        if (_id != 0) // account with name exists, we are done
            return next;

        // try parsing as account id instead
        if (uint.TryParse(text, out uint id))
            return ChatCommandResult.FromErrorMessage(handler.GetParsedString(CypherStrings.CmdparserAccountNameNoExist, _name));

        _id = id;
        _session = _worldManager.FindSession(_id);

        return _accountManager.GetName(_id, out _name) ? next : ChatCommandResult.FromErrorMessage(handler.GetParsedString(CypherStrings.CmdparserAccountIdNoExist, _id));
    }
}