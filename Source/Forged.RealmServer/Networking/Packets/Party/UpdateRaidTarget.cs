// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

class UpdateRaidTarget : ClientPacket
{
	public sbyte PartyIndex;
	public ObjectGuid Target;
	public sbyte Symbol;
	public UpdateRaidTarget(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PartyIndex = _worldPacket.ReadInt8();
		Target = _worldPacket.ReadPackedGuid();
		Symbol = _worldPacket.ReadInt8();
	}
}