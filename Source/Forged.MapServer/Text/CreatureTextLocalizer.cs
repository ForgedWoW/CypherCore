// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Players;
using Framework.Constants;

namespace Forged.MapServer.Text;

public class CreatureTextLocalizer : IDoWork<Player>
{
    private readonly MessageBuilder _builder;
    private readonly ChatMsg _msgType;
    private readonly Dictionary<Locale, ChatPacketSender> _packetCache = new();

    public CreatureTextLocalizer(MessageBuilder builder, ChatMsg msgType)
    {
        _builder = builder;
        _msgType = msgType;
    }

    public void Invoke(Player player)
    {
        var locIdx = player.Session.SessionDbLocaleIndex;
        ChatPacketSender sender;

        // create if not cached yet
        if (!_packetCache.ContainsKey(locIdx))
        {
            sender = _builder.Invoke(locIdx);
            _packetCache[locIdx] = sender;
        }
        else
        {
            sender = _packetCache[locIdx];
        }

        switch (_msgType)
        {
            case ChatMsg.MonsterWhisper:
            case ChatMsg.RaidBossWhisper:
                var message = sender.UntranslatedPacket;
                message.SetReceiver(player, locIdx);
                player.SendPacket(message);

                break;
        }

        sender.Invoke(player);
    }
}