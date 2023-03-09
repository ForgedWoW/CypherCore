// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class PetitionSignResults : ServerPacket
{
	public ObjectGuid Item;
	public ObjectGuid Player;
	public PetitionSigns Error = 0;
	public PetitionSignResults() : base(ServerOpcodes.PetitionSignResults) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Item);
		_worldPacket.WritePackedGuid(Player);

		_worldPacket.WriteBits(Error, 4);
		_worldPacket.FlushBits();
	}
}