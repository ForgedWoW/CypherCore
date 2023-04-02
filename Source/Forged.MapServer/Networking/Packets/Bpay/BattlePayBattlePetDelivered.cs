// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Bpay;

public class BattlePayBattlePetDelivered : ServerPacket
{
    public BattlePayBattlePetDelivered() : base(ServerOpcodes.BattlePayBattlePetDelivered) { }

    public ObjectGuid BattlePetGuid { get; set; } = new();
    public uint DisplayID { get; set; } = 0;
    /*WorldPacket const* WorldPackets::BattlePay::PurchaseDetails::Write()
    {
        _worldPacket << UnkInt;
        _worldPacket << VasPurchaseProgress;
        _worldPacket << UnkLong;
   
        _worldPacket.WriteBits(Key.length(), 6);
        _worldPacket.WriteBits(Key2.length(), 6);
        _worldPacket.WriteString(Key);
        _worldPacket.WriteString(Key2);
   
        return &_worldPacket;
    }*/

    /*WorldPacket const* WorldPackets::BattlePay::PurchaseUnk::Write()
    {
        _worldPacket << UnkByte;
        _worldPacket << UnkInt;
   
        _worldPacket.WriteBits(Key.length(), 7);
        _worldPacket.WriteString(Key);
   
        return &_worldPacket;
    }*/

    public override void Write()
    {
        WorldPacket.Write(DisplayID);
        WorldPacket.Write(BattlePetGuid);
    }
}