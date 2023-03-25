// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets.Bpay;

public class BattlePayStartDistributionAssignToTargetResponse : ServerPacket
{
	public ulong DistributionID { get; set; } = 0;
	public uint unkint1 { get; set; } = 0;
	public uint unkint2 { get; set; } = 0;

	public BattlePayStartDistributionAssignToTargetResponse() : base(ServerOpcodes.BattlePayStartDistributionAssignToTargetResponse) { }


	/*WorldPacket const* WorldPackets::BattlePay::BattlepayUnk::Write()
	{
		_worldPacket << UnkInt;

		return &_worldPacket;
	}*/

	public override void Write()
	{
		_worldPacket.Write(DistributionID);
		_worldPacket.Write(unkint1);
		_worldPacket.Write(unkint2);
	}
}