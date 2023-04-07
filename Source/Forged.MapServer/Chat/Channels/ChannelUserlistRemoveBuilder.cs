// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps.Workers;
using Forged.MapServer.Networking.Packets.Channel;
using Forged.MapServer.Text;
using Framework.Constants;

namespace Forged.MapServer.Chat.Channels;

internal class ChannelUserlistRemoveBuilder : MessageBuilder
{
    private readonly ObjectGuid _guid;
    private readonly Channel _source;
    public ChannelUserlistRemoveBuilder(Channel source, ObjectGuid guid)
    {
        _source = source;
        _guid = guid;
    }

    public override PacketSenderOwning<UserlistRemove> Invoke(Locale locale = Locale.enUS)
    {
        var localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

        PacketSenderOwning<UserlistRemove> userlistRemove = new()
        {
            Data =
            {
                RemovedUserGUID = _guid,
                ChannelFlags = _source.Flags,
                ChannelID = _source.ChannelId,
                ChannelName = _source.GetName(localeIdx)
            }
        };

        return userlistRemove;
    }
}