// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Petition;

public class PetitionAlreadySigned : ServerPacket
{
	public ObjectGuid SignerGUID;
	public PetitionAlreadySigned() : base(ServerOpcodes.PetitionAlreadySigned) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(SignerGUID);
	}
}