// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.BattlePet;

public class BattlePetDeleted : ServerPacket
{
	public ObjectGuid PetGuid;
	public BattlePetDeleted() : base(ServerOpcodes.BattlePetDeleted) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(PetGuid);
	}
}
