// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Maps.Workers;
using Forged.MapServer.Networking.Packets.Channel;
using Forged.MapServer.Text;
using Forged.MapServer.World;
using Framework.Constants;

namespace Forged.MapServer.Chat.Channels;

internal class ChannelNotifyJoinedBuilder : MessageBuilder
{
    private readonly Channel _source;
    private readonly WorldManager _worldManager;

    public ChannelNotifyJoinedBuilder(Channel source, WorldManager worldManager)
    {
        _source = source;
        _worldManager = worldManager;
    }

    public override PacketSenderOwning<ChannelNotifyJoined> Invoke(Locale locale = Locale.enUS)
    {
        var localeIdx = _worldManager.GetAvailableDbcLocale(locale);

        PacketSenderOwning<ChannelNotifyJoined> notify = new()
        {
            Data =
            {
                //notify.ChannelWelcomeMsg = "";
                ChatChannelID = (int)_source.ChannelId,
                //notify.InstanceID = 0;
                ChannelFlags = _source.Flags,
                Channel = _source.GetName(localeIdx),
                ChannelGUID = _source.GUID
            }
        };

        return notify;
    }
}