// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Networking;

namespace Forged.RealmServer.Maps;

public class PacketSenderOwning<T> : IDoWork<Player> where T : ServerPacket, new()
{
	public T Data { get; set; } = new();

	public void Invoke(Player player)
	{
		player.SendPacket(Data);
	}
}