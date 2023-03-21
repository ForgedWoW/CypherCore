// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Networking;

namespace Forged.RealmServer.Maps;

public class PacketSenderRef : IDoWork<Player>
{
	readonly ServerPacket _data;

	public PacketSenderRef(ServerPacket message)
	{
		_data = message;
	}

	public virtual void Invoke(Player player)
	{
		player.SendPacket(_data);
	}
}