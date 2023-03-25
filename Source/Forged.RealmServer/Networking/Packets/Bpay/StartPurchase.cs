// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets.Bpay;

public class StartPurchase : ClientPacket
{
	public ObjectGuid TargetCharacter { get; set; } = new();
	public uint ClientToken { get; set; } = 0;
	public uint ProductID { get; set; } = 0;

	public StartPurchase(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ClientToken = _worldPacket.ReadUInt32();
		ProductID = _worldPacket.ReadUInt32();
		TargetCharacter = _worldPacket.ReadPackedGuid();
	}
}