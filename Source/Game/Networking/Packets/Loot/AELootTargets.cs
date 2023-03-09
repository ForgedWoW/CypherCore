// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

class AELootTargets : ServerPacket
{
	readonly uint Count;

	public AELootTargets(uint count) : base(ServerOpcodes.AeLootTargets, ConnectionType.Instance)
	{
		Count = count;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32(Count);
	}
}