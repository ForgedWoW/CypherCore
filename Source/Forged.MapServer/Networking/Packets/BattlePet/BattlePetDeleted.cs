// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattlePet;

class BattlePetDeleted : ServerPacket
{
	public ObjectGuid PetGuid;
	public BattlePetDeleted() : base(ServerOpcodes.BattlePetDeleted) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(PetGuid);
	}
}