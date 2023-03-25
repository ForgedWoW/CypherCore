// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Movement;

public class WorldPortResponse : ClientPacket
{
	public WorldPortResponse(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}