// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Framework.Constants;

namespace Forged.MapServer.Chat;

class PlayerIdentifier
{
	string _name;
	ObjectGuid _guid;
	Player _player;

	public PlayerIdentifier() { }

	public PlayerIdentifier(Player player)
	{
		_name = player.GetName();
		_guid = player.GUID;
		_player = player;
	}

	public string GetName()
	{
		return _name;
	}

	public ObjectGuid GetGUID()
	{
		return _guid;
	}

	public bool IsConnected()
	{
		return _player != null;
	}

	public Player GetConnectedPlayer()
	{
		return _player;
	}

	public static PlayerIdentifier FromTarget(CommandHandler handler)
	{
		var player = handler.Player;

		if (player != null)
		{
			var target = player.SelectedPlayer;

			if (target != null)
				return new PlayerIdentifier(target);
		}

		return null;
	}

	public static PlayerIdentifier FromSelf(CommandHandler handler)
	{
		var player = handler.Player;

		if (player != null)
			return new PlayerIdentifier(player);

		return null;
	}

	public static PlayerIdentifier FromTargetOrSelf(CommandHandler handler)
	{
		var fromTarget = FromTarget(handler);

		if (fromTarget != null)
			return fromTarget;
		else
			return FromSelf(handler);
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
		else
		{
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
}