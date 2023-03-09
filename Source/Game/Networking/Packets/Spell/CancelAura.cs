// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

class CancelAura : ClientPacket
{
	public ObjectGuid CasterGUID;
	public uint SpellID;
	public CancelAura(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		SpellID = _worldPacket.ReadUInt32();
		CasterGUID = _worldPacket.ReadPackedGuid();
	}
}

//Structs