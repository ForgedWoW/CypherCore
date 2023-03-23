// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Misc;

public class RandomRoll : ServerPacket
{
	public ObjectGuid Roller;
	public ObjectGuid RollerWowAccount;
	public int Min;
	public int Max;
	public int Result;

	public RandomRoll() : base(ServerOpcodes.RandomRoll) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Roller);
		_worldPacket.WritePackedGuid(RollerWowAccount);
		_worldPacket.WriteInt32(Min);
		_worldPacket.WriteInt32(Max);
		_worldPacket.WriteInt32(Result);
	}
}
