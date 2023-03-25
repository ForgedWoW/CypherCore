// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets.Bpay;

public class DistributionAssignToTarget : ClientPacket
{
	public ObjectGuid TargetCharacter { get; set; } = new();
	public ulong DistributionID { get; set; } = 0;
	public uint ProductID { get; set; } = 0;
	public ushort SpecializationID { get; set; } = 0;
	public ushort ChoiceID { get; set; } = 0;

	public DistributionAssignToTarget(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ProductID = _worldPacket.ReadUInt32();
		DistributionID = _worldPacket.ReadUInt64();
		TargetCharacter = _worldPacket.ReadPackedGuid();
		SpecializationID = _worldPacket.ReadUInt16();
		ChoiceID = _worldPacket.ReadUInt16();
	}
}