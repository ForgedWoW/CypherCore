// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class ResurrectResponse : ClientPacket
{
	public ObjectGuid Resurrecter;
	public uint Response;
	public ResurrectResponse(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Resurrecter = _worldPacket.ReadPackedGuid();
		Response = _worldPacket.ReadUInt32();
	}
}