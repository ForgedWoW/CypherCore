// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps.Workers;
using Forged.MapServer.Networking.Packets.Channel;
using Forged.MapServer.Text;
using Forged.MapServer.World;
using Framework.Constants;

namespace Forged.MapServer.Chat.Channels;

internal class ChannelUserlistAddBuilder : MessageBuilder
{
    private readonly ObjectGuid _guid;
    private readonly WorldManager _worldManager;
    private readonly Channel _source;
    public ChannelUserlistAddBuilder(Channel source, ObjectGuid guid, WorldManager worldManager)
    {
        _source = source;
        _guid = guid;
        _worldManager = worldManager;
    }

    public override PacketSenderOwning<UserlistAdd> Invoke(Locale locale = Locale.enUS)
    {
        var localeIdx = _worldManager.GetAvailableDbcLocale(locale);

        PacketSenderOwning<UserlistAdd> userlistAdd = new()
        {
            Data =
            {
                AddedUserGUID = _guid,
                ChannelFlags = _source.Flags,
                UserFlags = _source.GetPlayerFlags(_guid),
                ChannelID = _source.ChannelId,
                ChannelName = _source.GetName(localeIdx)
            }
        };

        return userlistAdd;
    }
}