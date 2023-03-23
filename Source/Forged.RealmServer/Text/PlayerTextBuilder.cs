// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Chat;
using Game.Entities;
using Forged.RealmServer.Maps;
using Game.Networking.Packets;

namespace Forged.RealmServer;

public class PlayerTextBuilder : MessageBuilder
{
	readonly WorldObject _source;
	readonly WorldObject _talker;
	readonly Gender _gender;
	readonly ChatMsg _msgType;
	readonly byte _textGroup;
	readonly uint _textId;
	readonly Language _language;
	readonly WorldObject _target;

	public PlayerTextBuilder(WorldObject obj, WorldObject speaker, Gender gender, ChatMsg msgtype, byte textGroup, uint id, Language language, WorldObject target)
	{
		_source = obj;
		_gender = gender;
		_talker = speaker;
		_msgType = msgtype;
		_textGroup = textGroup;
		_textId = id;
		_language = language;
		_target = target;
	}

	public override PacketSenderOwning<ChatPkt> Invoke(Locale loc_idx = Locale.enUS)
	{
		var text = Global.CreatureTextMgr.GetLocalizedChatString(_source.Entry, _gender, _textGroup, _textId, loc_idx);
		PacketSenderOwning<ChatPkt> chat = new();
		chat.Data.Initialize(_msgType, _language, _talker, _target, text, 0, "", loc_idx);

		return chat;
	}
}