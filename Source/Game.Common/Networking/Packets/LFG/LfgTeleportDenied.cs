// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

public class LfgTeleportDenied : ServerPacket
{
	public LfgTeleportResult Reason;

	public LfgTeleportDenied(LfgTeleportResult reason) : base(ServerOpcodes.LfgTeleportDenied, ConnectionType.Instance)
	{
		Reason = reason;
	}

	public override void Write()
	{
		_worldPacket.WriteBits(Reason, 4);
		_worldPacket.FlushBits();
	}
}