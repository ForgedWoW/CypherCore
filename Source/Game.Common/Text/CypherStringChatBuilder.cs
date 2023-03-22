// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Chat;

class CypherStringChatBuilder : MessageBuilder
{
	readonly WorldObject _source;
	readonly ChatMsg _msgType;
	readonly CypherStrings _textId;
	readonly WorldObject _target;
	readonly object[] _args;

	public CypherStringChatBuilder(WorldObject obj, ChatMsg msgType, CypherStrings textId, WorldObject target = null, object[] args = null)
	{
		_source = obj;
		_msgType = msgType;
		_textId = textId;
		_target = target;
		_args = args;
	}

	public override ChatPacketSender Invoke(Locale locale)
	{
		var text = Global.ObjectMgr.GetCypherString(_textId, locale);

		if (_args != null)
			return new ChatPacketSender(_msgType, Language.Universal, _source, _target, string.Format(text, _args), 0, locale);
		else
			return new ChatPacketSender(_msgType, Language.Universal, _source, _target, text, 0, locale);
	}
}