// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Pet;

class PetStableResult : ServerPacket
{
	public StableResult Result;
	public PetStableResult() : base(ServerOpcodes.PetStableResult, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt8((byte)Result);
	}
}