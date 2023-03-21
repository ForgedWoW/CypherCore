// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class ServerPetitionShowList : ServerPacket
{
	public ObjectGuid Unit;
	public uint Price = 0;
	public ServerPetitionShowList() : base(ServerOpcodes.PetitionShowList) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Unit);
		_worldPacket.WriteUInt32(Price);
	}
}