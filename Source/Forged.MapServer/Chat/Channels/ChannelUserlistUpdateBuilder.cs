// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps.Workers;
using Forged.MapServer.Networking.Packets.Channel;
using Forged.MapServer.Text;
using Forged.MapServer.World;
using Framework.Constants;

namespace Forged.MapServer.Chat.Channels;

internal class ChannelUserlistUpdateBuilder : MessageBuilder
{
    private readonly ObjectGuid _guid;
    private readonly Channel _source;
    private readonly WorldManager _worldManager;

    public ChannelUserlistUpdateBuilder(Channel source, ObjectGuid guid, WorldManager worldManager)
    {
        _source = source;
        _guid = guid;
        _worldManager = worldManager;
    }

    public override PacketSenderOwning<UserlistUpdate> Invoke(Locale locale = Locale.enUS)
    {
        var localeIdx = _worldManager.GetAvailableDbcLocale(locale);

        PacketSenderOwning<UserlistUpdate> userlistUpdate = new()
        {
            Data =
            {
                UpdatedUserGUID = _guid,
                ChannelFlags = _source.Flags,
                UserFlags = _source.GetPlayerFlags(_guid),
                ChannelID = _source.ChannelId,
                ChannelName = _source.GetName(localeIdx)
            }
        };

        return userlistUpdate;
    }
}