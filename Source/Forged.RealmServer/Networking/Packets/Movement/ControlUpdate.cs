// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class ControlUpdate : ServerPacket
{
	public bool On;
	public ObjectGuid Guid;
	public ControlUpdate() : base(ServerOpcodes.ControlUpdate) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Guid);
		_worldPacket.WriteBit(On);
		_worldPacket.FlushBits();
	}
}