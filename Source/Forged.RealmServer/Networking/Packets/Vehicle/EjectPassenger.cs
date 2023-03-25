// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class EjectPassenger : ClientPacket
{
	public ObjectGuid Passenger;
	public EjectPassenger(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Passenger = _worldPacket.ReadPackedGuid();
	}
}