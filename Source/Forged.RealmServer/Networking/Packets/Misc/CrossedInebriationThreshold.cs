// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

class CrossedInebriationThreshold : ServerPacket
{
	public ObjectGuid Guid;
	public uint ItemID;
	public uint Threshold;
	public CrossedInebriationThreshold() : base(ServerOpcodes.CrossedInebriationThreshold) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Guid);
		_worldPacket.WriteUInt32(Threshold);
		_worldPacket.WriteUInt32(ItemID);
	}
}