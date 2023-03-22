﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Chat;

public class CustomChatTextBuilder : MessageBuilder
{
	readonly WorldObject _source;
	readonly ChatMsg _msgType;
	readonly string _text;
	readonly Language _language;
	readonly WorldObject _target;

	public CustomChatTextBuilder(WorldObject obj, ChatMsg msgType, string text, Language language = Language.Universal, WorldObject target = null)
	{
		_source = obj;
		_msgType = msgType;
		_text = text;
		_language = language;
		_target = target;
	}

	public override ChatPacketSender Invoke(Locale locale)
	{
		return new ChatPacketSender(_msgType, _language, _source, _target, _text, 0, locale);
	}
}