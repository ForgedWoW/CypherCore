// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class PetCastSpell : ClientPacket
{
	public ObjectGuid PetGUID;
	public SpellCastRequest Cast;

	public PetCastSpell(WorldPacket packet) : base(packet)
	{
		Cast = new SpellCastRequest();
	}

	public override void Read()
	{
		PetGUID = _worldPacket.ReadPackedGuid();
		Cast.Read(_worldPacket);
	}
}