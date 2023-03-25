// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class OfferPetitionError : ServerPacket
{
	public ObjectGuid PlayerGUID;
	public OfferPetitionError() : base(ServerOpcodes.OfferPetitionError) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(PlayerGUID);
	}
}