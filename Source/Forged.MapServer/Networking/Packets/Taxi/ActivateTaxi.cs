// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

class ActivateTaxi : ClientPacket
{
	public ObjectGuid Vendor;
	public uint Node;
	public uint GroundMountID;
	public uint FlyingMountID;
	public ActivateTaxi(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Vendor = _worldPacket.ReadPackedGuid();
		Node = _worldPacket.ReadUInt32();
		GroundMountID = _worldPacket.ReadUInt32();
		FlyingMountID = _worldPacket.ReadUInt32();
	}
}