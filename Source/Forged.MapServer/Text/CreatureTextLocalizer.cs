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
        var loc_idx = player.Session.SessionDbLocaleIndex;
        ChatPacketSender sender;

        // create if not cached yet
        if (!_packetCache.ContainsKey(loc_idx))
        {
            sender = _builder.Invoke(loc_idx);
            _packetCache[loc_idx] = sender;
        }
        else
        {
            sender = _packetCache[loc_idx];
        }

        switch (_msgType)
        {
            case ChatMsg.MonsterWhisper:
            case ChatMsg.RaidBossWhisper:
                var message = sender.UntranslatedPacket;
                message.SetReceiver(player, loc_idx);
                player.SendPacket(message);

                break;
            default:
                break;
        }

        sender.Invoke(player);
    }
}