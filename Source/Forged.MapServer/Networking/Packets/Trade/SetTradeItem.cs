// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Trade;

public class SetTradeItem : ClientPacket
{
    public byte ItemSlotInPack;
    public byte PackSlot;
    public byte TradeSlot;
    public SetTradeItem(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        TradeSlot = WorldPacket.ReadUInt8();
        PackSlot = WorldPacket.ReadUInt8();
        ItemSlotInPack = WorldPacket.ReadUInt8();
    }
}