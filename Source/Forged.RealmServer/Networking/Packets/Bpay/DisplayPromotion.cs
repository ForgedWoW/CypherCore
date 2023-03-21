// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets.Bpay;

public class DisplayPromotion : ServerPacket
{
	public uint PromotionID { get; set; } = 0;

	public DisplayPromotion(uint ID) : base(ServerOpcodes.DisplayPromotion)
	{
		PromotionID = ID;
	}


	/*void WorldPackets::BattlePay::PurchaseDetailsResponse::Read()
	{
		_worldPacket >> UnkByte;
	}*/

	/*/
	void WorldPackets::BattlePay::PurchaseUnkResponse::Read()
	{
		auto keyLen = _worldPacket.ReadBits(6);
		auto key2Len = _worldPacket.ReadBits(7);
		Key = _worldPacket.ReadString(keyLen);
		Key2 = _worldPacket.ReadString(key2Len);
	}*/

	public override void Write()
	{
		_worldPacket.Write(PromotionID);
	}
}