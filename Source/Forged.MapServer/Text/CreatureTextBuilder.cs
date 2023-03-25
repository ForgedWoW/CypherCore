// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Text;

public class CreatureTextBuilder : MessageBuilder
{
	readonly WorldObject _source;
	readonly Gender _gender;
	readonly ChatMsg _msgType;
	readonly byte _textGroup;
	readonly uint _textId;
	readonly Language _language;
	readonly WorldObject _target;

	public CreatureTextBuilder(WorldObject obj, Gender gender, ChatMsg msgtype, byte textGroup, uint id, Language language, WorldObject target)
	{
		_source = obj;
		_gender = gender;
		_msgType = msgtype;
		_textGroup = textGroup;
		_textId = id;
		_language = language;
		_target = target;
	}

	public override ChatPacketSender Invoke(Locale locale = Locale.enUS)
	{
		var text = Global.CreatureTextMgr.GetLocalizedChatString(_source.Entry, _gender, _textGroup, _textId, locale);

		return new ChatPacketSender(_msgType, _language, _source, _target, text, 0, locale);
	}
}