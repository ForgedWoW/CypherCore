// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

class BattlePetDeletePet : ClientPacket
{
	public ObjectGuid PetGuid;
	public BattlePetDeletePet(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PetGuid = _worldPacket.ReadPackedGuid();
	}
}