// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.Petition;

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
