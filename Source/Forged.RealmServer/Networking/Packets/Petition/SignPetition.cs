// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class SignPetition : ClientPacket
{
	public ObjectGuid PetitionGUID;
	public byte Choice;
	public SignPetition(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PetitionGUID = _worldPacket.ReadPackedGuid();
		Choice = _worldPacket.ReadUInt8();
	}
}