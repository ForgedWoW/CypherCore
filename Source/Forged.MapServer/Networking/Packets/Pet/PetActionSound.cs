// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class PetActionSound : ServerPacket
{
	public ObjectGuid UnitGUID;
	public PetTalk Action;
	public PetActionSound() : base(ServerOpcodes.PetStableResult) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(UnitGUID);
		_worldPacket.WriteUInt32((uint)Action);
	}
}