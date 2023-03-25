// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.VoidStorage;

class VoidStorageFailed : ServerPacket
{
	public byte Reason = 0;
	public VoidStorageFailed() : base(ServerOpcodes.VoidStorageFailed, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt8(Reason);
	}
}