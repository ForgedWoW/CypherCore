// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps.Workers;
using Forged.MapServer.Networking.Packets.Chat;
using Forged.MapServer.Text;
using Framework.Constants;

namespace Forged.MapServer.Chat.Channels;

internal class ChannelWhisperBuilder : MessageBuilder
{
    private readonly ObjectGuid _guid;
    private readonly Language _lang;
    private readonly string _prefix;
    private readonly Channel _source;
    private readonly string _what;
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