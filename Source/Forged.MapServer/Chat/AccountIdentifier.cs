// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Chat;

class AccountIdentifier
{
	uint _id;
	string _name;
	WorldSession _session;

	public AccountIdentifier() { }

	public AccountIdentifier(WorldSession session)
	{
		_id = session.AccountId;
		_name = session.AccountName;
		_session = session;
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

	public WorldSession GetConnectedSession()
	{
		return _session;
	}

	public ChatCommandResult TryConsume(CommandHandler handler, string args)
	{
		var next = CommandArgs.TryConsume(out var text, typeof(string), handler, args);

		if (!next.IsSuccessful)
			return next;

		// first try parsing as account name
		_name = text;
		_id = Global.AccountMgr.GetId(_name);
		_session = Global.WorldMgr.FindSession(_id);

		if (_id != 0) // account with name exists, we are done
			return next;

		// try parsing as account id instead
		if (uint.TryParse(text, out uint id))
			return ChatCommandResult.FromErrorMessage(handler.GetParsedString(CypherStrings.CmdparserAccountNameNoExist, _name));

		_id = id;
		_session = Global.WorldMgr.FindSession(_id);

		if (Global.AccountMgr.GetName(_id, out _name))
			return next;
		else
			return ChatCommandResult.FromErrorMessage(handler.GetParsedString(CypherStrings.CmdparserAccountIdNoExist, _id));
	}

	public static AccountIdentifier FromTarget(CommandHandler handler)
	{
		var player = handler.Player;

		if (player != null)
		{
			var target = player.SelectedPlayer;

			if (target != null)
			{
				var session = target.Session;

				if (session != null)
					return new AccountIdentifier(session);
			}
		}

		return null;
	}
}