// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class OfferPetition : ClientPacket
{
	public ObjectGuid TargetPlayer;
	public ObjectGuid ItemGUID;
	public OfferPetition(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ItemGUID = _worldPacket.ReadPackedGuid();
		TargetPlayer = _worldPacket.ReadPackedGuid();
	}
}