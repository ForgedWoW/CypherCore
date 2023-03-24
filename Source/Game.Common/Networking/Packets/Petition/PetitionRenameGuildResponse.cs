// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.Petition;

public class PetitionRenameGuildResponse : ServerPacket
{
	public ObjectGuid PetitionGuid;
	public string NewGuildName;
	public PetitionRenameGuildResponse() : base(ServerOpcodes.PetitionRenameGuildResponse) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(PetitionGuid);

		_worldPacket.WriteBits(NewGuildName.GetByteCount(), 7);
		_worldPacket.FlushBits();

		_worldPacket.WriteString(NewGuildName);
	}
}
