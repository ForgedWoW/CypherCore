// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Forged.RealmServer.Maps;
using Game.Common.Entities.Objects;
using Game.Common.Networking.Packets.Chat;

namespace Forged.RealmServer.Chat;

class ChannelWhisperBuilder : MessageBuilder
{
	readonly Channel _source;
	readonly Language _lang;
	readonly string _what;
	readonly string _prefix;
	readonly ObjectGuid _guid;

	public ChannelWhisperBuilder(Channel source, Language lang, string what, string prefix, ObjectGuid guid)
	{
		_source = source;
		_lang = lang;
		_what = what;
		_prefix = prefix;
		_guid = guid;
	}

	public override PacketSenderOwning<ChatPkt> Invoke(Locale locale = Locale.enUS)
	{
		var localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

		PacketSenderOwning<ChatPkt> packet = new();
		var player = Global.ObjAccessor.FindConnectedPlayer(_guid);

		if (player)
		{
			packet.Data.Initialize(ChatMsg.Channel, _lang, player, player, _what, 0, _source.GetName(localeIdx), Locale.enUS, _prefix);
		}
		else
		{
			packet.Data.Initialize(ChatMsg.Channel, _lang, null, null, _what, 0, _source.GetName(localeIdx), Locale.enUS, _prefix);
			packet.Data.SenderGUID = _guid;
			packet.Data.TargetGUID = _guid;
		}

		return packet;
	}
}