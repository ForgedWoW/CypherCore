// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Players;
using Game.Common.Text;
using Game;

namespace Game.Common.Text;

public class CreatureTextLocalizer : IDoWork<Player>
{
	readonly Dictionary<Locale, ChatPacketSender> _packetCache = new();
	readonly MessageBuilder _builder;
	readonly ChatMsg _msgType;

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
