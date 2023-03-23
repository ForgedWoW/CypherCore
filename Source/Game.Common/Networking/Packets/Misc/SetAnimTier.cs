// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Misc;

public class SetAnimTier : ServerPacket
{
	public ObjectGuid Unit;
	public int Tier;
	public SetAnimTier() : base(ServerOpcodes.SetAnimTier, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Unit);
		_worldPacket.WriteBits(Tier, 3);
		_worldPacket.FlushBits();
	}
}
