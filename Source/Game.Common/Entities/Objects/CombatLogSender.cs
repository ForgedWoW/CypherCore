// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;
using Game.Common.Entities.Players;
using Game.Common.Networking.Packets.CombatLog;

namespace Game.Common.Entities.Objects;

internal class CombatLogSender : IDoWork<Player>
{
	readonly CombatLogServerPacket _message;

	public CombatLogSender(CombatLogServerPacket msg)
	{
		_message = msg;
	}

	public void Invoke(Player player)
	{
		_message.Clear();
		_message.SetAdvancedCombatLogging(player.IsAdvancedCombatLoggingEnabled);

		player.SendPacket(_message);
	}
}
