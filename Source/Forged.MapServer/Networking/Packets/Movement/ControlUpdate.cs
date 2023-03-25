// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Movement;

public class ControlUpdate : ServerPacket
{
	public bool On;
	public ObjectGuid Guid;
	public ControlUpdate() : base(ServerOpcodes.ControlUpdate) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Guid);
		_worldPacket.WriteBit(On);
		_worldPacket.FlushBits();
	}
}