// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Trade;

public class SetTradeCurrency : ClientPacket
{
    public uint Quantity;
    public uint Type;
    public SetTradeCurrency(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Type = WorldPacket.ReadUInt32();
        Quantity = WorldPacket.ReadUInt32();
    }
}