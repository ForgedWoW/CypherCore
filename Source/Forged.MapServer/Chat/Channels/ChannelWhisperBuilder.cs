// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps.Workers;
using Forged.MapServer.Networking.Packets.Chat;
using Forged.MapServer.Text;
using Forged.MapServer.World;
using Framework.Constants;

namespace Forged.MapServer.Chat.Channels;

internal class ChannelWhisperBuilder : MessageBuilder
{
    private readonly ObjectGuid _guid;
    private readonly Language _lang;
    private readonly ObjectAccessor _objectAccessor;
    private readonly string _prefix;
    private readonly Channel _source;
    private readonly string _what;
    private readonly WorldManager _worldManager;

    public ChannelWhisperBuilder(Channel source, Language lang, string what, string prefix, ObjectGuid guid, WorldManager worldManager, ObjectAccessor objectAccessor)
    {
        _source = source;
        _lang = lang;
        _what = what;
        _prefix = prefix;
        _guid = guid;
        _worldManager = worldManager;
        _objectAccessor = objectAccessor;
    }

    public override PacketSenderOwning<ChatPkt> Invoke(Locale locale = Locale.enUS)
    {
        var localeIdx = _worldManager.GetAvailableDbcLocale(locale);

        PacketSenderOwning<ChatPkt> packet = new();
        var player = _objectAccessor.FindConnectedPlayer(_guid);

        if (player != null)
            packet.Data.Initialize(ChatMsg.Channel, _lang, player, player, _what, 0, _source.GetName(localeIdx), Locale.enUS, _prefix);
        else
        {
            packet.Data.Initialize(ChatMsg.Channel, _lang, null, null, _what, 0, _source.GetName(localeIdx), Locale.enUS, _prefix);
            packet.Data.SenderGUID = _guid;
            packet.Data.TargetGUID = _guid;
        }

        return packet;
    }
}