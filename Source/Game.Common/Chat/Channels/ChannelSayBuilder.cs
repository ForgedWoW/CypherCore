// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Common.Chat.Channels;
using Game.Common.Entities.Objects;
using Game.Common.Networking.Packets.Chat;
using Game.Common.Text;

namespace Game.Common.Chat.Channels;

class ChannelSayBuilder : MessageBuilder
{
	readonly Channel _source;
	readonly Language _lang;
	readonly string _what;
	readonly ObjectGuid _guid;
	readonly ObjectGuid _channelGuid;

	public ChannelSayBuilder(Channel source, Language lang, string what, ObjectGuid guid, ObjectGuid channelGuid)
	{
		_source = source;
		_lang = lang;
		_what = what;
		_guid = guid;
		_channelGuid = channelGuid;
	}

	public override PacketSenderOwning<ChatPkt> Invoke(Locale locale = Locale.enUS)
	{
		var localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

		PacketSenderOwning<ChatPkt> packet = new();
		var player = Global.ObjAccessor.FindConnectedPlayer(_guid);

		if (player)
		{
			packet.Data.Initialize(ChatMsg.Channel, _lang, player, player, _what, 0, _source.GetName(localeIdx));
		}
		else
		{
			packet.Data.Initialize(ChatMsg.Channel, _lang, null, null, _what, 0, _source.GetName(localeIdx));
			packet.Data.SenderGUID = _guid;
			packet.Data.TargetGUID = _guid;
		}

		packet.Data.ChannelGUID = _channelGuid;

		return packet;
	}
}
