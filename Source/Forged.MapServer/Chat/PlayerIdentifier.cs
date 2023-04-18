// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Framework.Constants;

namespace Forged.MapServer.Chat;

internal class PlayerIdentifier
{
    private ObjectGuid _guid;
    private string _name;
    private Player _player;

    public PlayerIdentifier() { }

    public PlayerIdentifier(Player player)
    {
        _name = player.GetName();
        _guid = player.GUID;
        _player = player;
    }

    public static PlayerIdentifier FromSelf(CommandHandler handler)
    {
        var player = handler.Player;

        if (player != null)
            return new PlayerIdentifier(player);

        return null;
    }

    public static PlayerIdentifier FromTarget(CommandHandler handler)
    {
        var player = handler.Player;

        var target = player?.SelectedPlayer;

        if (target != null)
            return new PlayerIdentifier(target);

        return null;
    }

    public static PlayerIdentifier FromTargetOrSelf(CommandHandler handler)
    {
        var fromTarget = FromTarget(handler);

        if (fromTarget != null)
            return fromTarget;

        return FromSelf(handler);
    }

    public Player GetConnectedPlayer()
    {
        return _player;
    }

    public ObjectGuid GetGUID()
    {
        return _guid;
    }

    public string GetName()
    {
        return _name;
    }

    public bool IsConnected()
    {
        return _player != null;
    }

    public ChatCommandResult TryConsume(CommandHandler handler, string args)
    {
        var next = CommandArgs.TryConsume(out var tempVal, typeof(ulong), handler, args);

        if (!next.IsSuccessful)
            next = CommandArgs.TryConsume(out tempVal, typeof(string), handler, args);

        if (!next.IsSuccessful)
            return next;

        if (tempVal is ulong)
        {
            _guid = ObjectGuid.Create(HighGuid.Player, tempVal);

            if ((_player = Global.ObjAccessor.FindPlayerByLowGUID(_guid.Counter)) != null)
                _name = _player.GetName();
            else if (!Global.CharacterCacheStorage.GetCharacterNameByGuid(_guid, out _name))
                return ChatCommandResult.FromErrorMessage(handler.GetParsedString(CypherStrings.CmdparserCharGuidNoExist, _guid.ToString()));

            return next;
        }

        _name = tempVal;

        if (!GameObjectManager.NormalizePlayerName(ref _name))
            return ChatCommandResult.FromErrorMessage(handler.GetParsedString(CypherStrings.CmdparserCharNameInvalid, _name));

        if ((_player = Global.ObjAccessor.FindPlayerByName(_name)) != null)
            _guid = _player.GUID;
        else if ((_guid = Global.CharacterCacheStorage.GetCharacterGuidByName(_name)).IsEmpty)
            return ChatCommandResult.FromErrorMessage(handler.GetParsedString(CypherStrings.CmdparserCharNameNoExist, _name));

        return next;
    }
}