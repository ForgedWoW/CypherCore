// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Combat;

public class AttackSwingError : ServerPacket
{
	readonly AttackSwingErr Reason;

	public AttackSwingError(AttackSwingErr reason = AttackSwingErr.CantAttack) : base(ServerOpcodes.AttackSwingError)
	{
		Reason = reason;
	}

	public override void Write()
	{
		_worldPacket.WriteBits((uint)Reason, 3);
		_worldPacket.FlushBits();
	}
}