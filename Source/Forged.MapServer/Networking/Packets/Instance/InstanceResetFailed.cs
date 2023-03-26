// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Instance;

internal class InstanceResetFailed : ServerPacket
{
	public uint MapID;
	public ResetFailedReason ResetFailedReason;
	public InstanceResetFailed() : base(ServerOpcodes.InstanceResetFailed) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(MapID);
		_worldPacket.WriteBits(ResetFailedReason, 2);
		_worldPacket.FlushBits();
	}
}