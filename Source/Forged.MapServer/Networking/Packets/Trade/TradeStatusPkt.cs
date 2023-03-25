// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Trade;

public class TradeStatusPkt : ServerPacket
{
	public TradeStatus Status = TradeStatus.Initiated;
	public byte TradeSlot;
	public ObjectGuid PartnerAccount;
	public ObjectGuid Partner;
	public int CurrencyType;
	public int CurrencyQuantity;
	public bool FailureForYou;
	public InventoryResult BagResult;
	public uint ItemID;
	public uint Id;
	public bool PartnerIsSameBnetAccount;
	public TradeStatusPkt() : base(ServerOpcodes.TradeStatus, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteBit(PartnerIsSameBnetAccount);
		_worldPacket.WriteBits(Status, 5);

		switch (Status)
		{
			case TradeStatus.Failed:
				_worldPacket.WriteBit(FailureForYou);
				_worldPacket.WriteInt32((int)BagResult);
				_worldPacket.WriteUInt32(ItemID);

				break;
			case TradeStatus.Initiated:
				_worldPacket.WriteUInt32(Id);

				break;
			case TradeStatus.Proposed:
				_worldPacket.WritePackedGuid(Partner);
				_worldPacket.WritePackedGuid(PartnerAccount);

				break;
			case TradeStatus.WrongRealm:
			case TradeStatus.NotOnTaplist:
				_worldPacket.WriteUInt8(TradeSlot);

				break;
			case TradeStatus.NotEnoughCurrency:
			case TradeStatus.CurrencyNotTradable:
				_worldPacket.WriteInt32(CurrencyType);
				_worldPacket.WriteInt32(CurrencyQuantity);

				break;
			default:
				_worldPacket.FlushBits();

				break;
		}
	}
}