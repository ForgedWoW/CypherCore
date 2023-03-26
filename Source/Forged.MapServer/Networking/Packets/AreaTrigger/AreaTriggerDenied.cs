// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.AreaTrigger;

internal class AreaTriggerDenied : ServerPacket
{
	public int AreaTriggerID;
	public bool Entered;
	public AreaTriggerDenied() : base(ServerOpcodes.AreaTriggerDenied) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(AreaTriggerID);
		_worldPacket.WriteBit(Entered);
		_worldPacket.FlushBits();
	}
}