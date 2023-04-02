// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Trade;

public class TradeStatusPkt : ServerPacket
{
    public InventoryResult BagResult;
    public int CurrencyQuantity;
    public int CurrencyType;
    public bool FailureForYou;
    public uint Id;
    public uint ItemID;
    public ObjectGuid Partner;
    public ObjectGuid PartnerAccount;
    public bool PartnerIsSameBnetAccount;
    public TradeStatus Status = TradeStatus.Initiated;
    public byte TradeSlot;
    public TradeStatusPkt() : base(ServerOpcodes.TradeStatus, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteBit(PartnerIsSameBnetAccount);
        WorldPacket.WriteBits(Status, 5);

        switch (Status)
        {
            case TradeStatus.Failed:
                WorldPacket.WriteBit(FailureForYou);
                WorldPacket.WriteInt32((int)BagResult);
                WorldPacket.WriteUInt32(ItemID);

                break;
            case TradeStatus.Initiated:
                WorldPacket.WriteUInt32(Id);

                break;
            case TradeStatus.Proposed:
                WorldPacket.WritePackedGuid(Partner);
                WorldPacket.WritePackedGuid(PartnerAccount);

                break;
            case TradeStatus.WrongRealm:
            case TradeStatus.NotOnTaplist:
                WorldPacket.WriteUInt8(TradeSlot);

                break;
            case TradeStatus.NotEnoughCurrency:
            case TradeStatus.CurrencyNotTradable:
                WorldPacket.WriteInt32(CurrencyType);
                WorldPacket.WriteInt32(CurrencyQuantity);

                break;
            default:
                WorldPacket.FlushBits();

                break;
        }
    }
}